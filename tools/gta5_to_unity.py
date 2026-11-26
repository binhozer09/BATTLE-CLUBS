import argparse
import os
import re
import sys
import traceback
from pathlib import Path

import bpy


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--addon", required=True)
    parser.add_argument("--source", required=True)
    parser.add_argument("--model-output", required=True)
    parser.add_argument("--texture-output", required=True)
    return parser.parse_args(argv)


def safe_name(value):
    value = re.sub(r"[^A-Za-z0-9_.-]+", "_", value).strip("._")
    return value or "texture"


def main():
    args = parse_args()
    source = Path(args.source).resolve()
    model_output = Path(args.model_output).resolve()
    texture_output = Path(args.texture_output).resolve()
    addon = Path(args.addon).resolve()

    ydr = source / "fv_prop_bar.ydr"
    ytd_files = sorted(source.glob("*.ytd"))
    if not ydr.is_file():
        raise FileNotFoundError(f"Missing source model: {ydr}")
    if not ytd_files:
        raise FileNotFoundError(f"No YTD texture dictionaries found in {source}")

    model_output.mkdir(parents=True, exist_ok=True)
    texture_output.mkdir(parents=True, exist_ok=True)

    # Enable Sollumz through Blender's add-on registry. Calling register() directly
    # leaves preferences.addons["Sollumz"] unset and crashes during preference setup.
    sys.path.insert(0, str(addon.parent))
    import addon_utils
    addon_utils.enable("Sollumz", default_set=True, persistent=True)
    if "Sollumz" not in bpy.context.preferences.addons:
        raise RuntimeError("Blender did not register Sollumz as an enabled add-on.")

    # Sollumz's CI bootstrap installs the pinned szio and Windows-only PyMateria
    # dependencies, enabling native binary YDR/YTD import rather than XML-only import.
    from Sollumz import dependencies
    if not dependencies.IS_SZIO_AVAILABLE:
        raise RuntimeError("Sollumz szio dependency did not initialize.")
    if not dependencies.IS_SZIO_NATIVE_AVAILABLE:
        raise RuntimeError("PyMateria did not initialize; native binary YDR/YTD import is unavailable.")

    bpy.ops.wm.read_homefile(use_empty=True)

    files = [{"name": ydr.name}] + [{"name": p.name} for p in ytd_files]
    result = bpy.ops.sollumz.import_assets(
        directory=str(source),
        files=files,
        use_custom_settings=True,
        import_as_asset=False,
        split_by_group=True,
        dwd_import_external_skeleton="NO",
        dwd_import_external_skeleton_saved_path="",
        frag_import_vehicle_windows=False,
        ymap_skip_missing_entities=True,
        ymap_exclude_entities=False,
        ymap_box_occluders=False,
        ymap_model_occluders=False,
        ymap_car_generators=False,
        ymap_instance_entities=True,
        ytyp_mlo_instance_entities=True,
        textures_mode="IMPORT_DIR",
        textures_extract_custom_directory="",
    )
    if result != {"FINISHED"}:
        raise RuntimeError(f"Sollumz import did not finish: {result}")

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("YDR import produced no mesh objects; refusing to export an empty FBX.")

    # Save every imported DDS/image as PNG for Unity, and repoint materials to those PNGs.
    saved = []
    used_names = set()
    for image in list(bpy.data.images):
        if image.source not in {"FILE", "GENERATED"}:
            continue
        if image.size[0] <= 0 or image.size[1] <= 0:
            continue
        original = Path(bpy.path.abspath(image.filepath)) if image.filepath else Path("")
        if original.suffix.lower() not in {".dds", ".png", ".tga", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"}:
            continue

        stem = safe_name(original.stem if original.name.lower().endswith(".dds") else image.name)
        candidate = stem
        index = 1
        while candidate.lower() in used_names:
            index += 1
            candidate = f"{stem}_{index}"
        used_names.add(candidate.lower())
        png_path = texture_output / f"{candidate}.png"
        image.filepath_raw = str(png_path)
        image.file_format = "PNG"
        image.save()
        image.filepath = str(png_path)
        saved.append(png_path)

    if not saved:
        raise RuntimeError("Sollumz imported no usable texture images; refusing to publish incomplete assets.")

    # Export the complete imported drawable hierarchy, including embedded mesh bounds
    # where Sollumz exposes them as meshes. Unity can add MeshCollider components after review.
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    exportable = [obj for obj in bpy.context.scene.objects if obj.type in {"MESH", "ARMATURE", "EMPTY"}]
    for obj in exportable:
        obj.select_set(True)
    if bpy.context.view_layer.objects.active is None and exportable:
        bpy.context.view_layer.objects.active = exportable[0]

    fbx_path = model_output / "fv_prop_bar.fbx"
    export_result = bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"MESH", "ARMATURE", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    if export_result != {"FINISHED"} or not fbx_path.is_file() or fbx_path.stat().st_size < 1024:
        raise RuntimeError(f"FBX export failed or produced an invalid file: {export_result}")

    report = [
        "BATTLE-CLUBS GTA V asset conversion",
        f"Source model: {ydr.name} ({ydr.stat().st_size} bytes)",
        "Texture dictionaries:",
        *[f"- {p.name} ({p.stat().st_size} bytes)" for p in ytd_files],
        f"Imported scene mesh objects: {len(meshes)}",
        f"Exported FBX: {fbx_path.name} ({fbx_path.stat().st_size} bytes)",
        f"Exported PNG textures: {len(saved)}",
        "Collision note: embedded collision meshes, when present in the source YDR, are included as mesh geometry; Unity MeshCollider components still require setup/review.",
    ]
    (model_output / "CONVERSION_REPORT.txt").write_text("\n".join(report) + "\n", encoding="utf-8")
    print("\n".join(report))


if __name__ == "__main__":
    try:
        main()
    except Exception:
        traceback.print_exc()
        raise
