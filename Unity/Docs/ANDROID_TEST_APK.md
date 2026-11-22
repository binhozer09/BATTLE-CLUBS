# Android test APK

The workflow `.github/workflows/android-apk.yml` builds a temporary Android APK from
`BattleClubs_Community_Test.unity` using `BattleClubsAndroidCI.BuildAndroid`.

This is a development test scene, not the complete BATTLE CLUBS game. It currently
uses the existing procedural map systems and temporary primitive player visuals.
The APK is uploaded as the GitHub Actions artifact
`BATTLE-CLUBS-ANDROID-TEST-APK` after a successful Unity Android build.

Unity Personal authentication and license activation are required in the workflow.
