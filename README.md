# UE 5.7 Android Setup Manager

A small WPF utility to help prepare a Windows development machine for building Unreal Engine 5.7 Android projects. The tool automates downloads and verification steps, streams logs to the UI, and helps generate an Android keystore.

---

## Features

- Download Android Studio (Koala / 2024.1.x) with a streaming progress window.
- Download Oracle JDK 21 installer files (EXE and MSI) with progress.
- Set `JAVA_HOME` to a selected JDK folder and add the JDK `bin` to the user `PATH`.
- Verify installed JDK (`java -version`) and stream the output to the logs.
- Install Android SDK/NDK packages using the SDK `sdkmanager` (streams `sdkmanager` logs, attempts to uninstall platforms with API > 35 and install required packages).
- Verify Unreal Engine Android extras (checks for `SetupAndroid.bat`, `SetupAndroid.command`, `SetupAndroid.sh`).
- Run `SetupAndroid.bat` from the UE 5.7 install and stream script output.
- Generate an Android keystore using `keytool` with a simple keystore dialog to collect DN and password fields.
- All long-running operations stream logs into the embedded `OutputBox` in real time.

---

## UI Overview

The main window (`MainWindow`) provides buttons for common tasks:

- `Android Studio Download` — downloads the Android Studio installer and shows step-by-step post-download instructions in the logs.
- `Install Required SDK + NDK` — finds `sdkmanager` in your SDK and installs a subset of packages required for UE 5.7 (cmdline tools, platforms;android-35, build-tools 35.0.1, ndk r27.x).
- `Verify Unreal Engine Android Support` — reports if UE extras scripts are present.
- `Download JDK 21` — downloads provided JDK installer URLs to the user's `Downloads` folder.
- `Verify JDK 21` — runs `java -version` and prints the result.
- `Set JAVA_HOME` — opens a folder picker to choose the JDK root folder and sets the user `JAVA_HOME` and adds its `bin` to the user `PATH`.
- `Run SetupAndroid.bat` — runs the UE-provided setup script and streams output.
- `Generate Keystore` — opens a `KeystoreWindow` to collect details and runs `keytool` to create the keystore (stored in `Documents` by default).

All logs are shown in the `OutputBox` text area.

---

## Prerequisites

- Windows OS (the project uses Windows-specific paths and calls).
- Visual Studio (tested with Visual Studio Community 2026). Build target: `.NET Framework 4.8`.
- `sdkmanager` available in the Android SDK root (installed by Android Studio command-line tools).
- `keytool` available either in `JAVA_HOME\bin` or on `PATH` (comes with JDK).

Notes:
- Running installers and `SetupAndroid.bat` may require Administrator privileges.
- `sdkmanager` may require manual license acceptance (the tool does not automatically accept all licenses).

---

## Building

1. Open the solution in Visual Studio (the project file is `UE57AndroidManager.csproj`).
2. Build using the `Debug` or `Release` configuration. Target framework is `.NET Framework 4.8`.

---

## Running

1. Start the application from Visual Studio or run the built `exe` from `bin\\Debug`.
2. Use the provided buttons to perform tasks. Check the `Logs` area for streamed output and instructions.

Typical workflow for preparing a machine for UE 5.7 Android development:

1. Click `Download JDK 21` (or install JDK manually) and then `Set JAVA_HOME` to the JDK installation folder.
2. Click `Download Android Studio` and run the installer. Note the Android SDK path used by the installer.
3. Click `Install Required SDK + NDK` to ensure required packages are present.
4. Verify Unreal Engine Android support via `Verify Unreal Engine Android Support` and run `SetupAndroid.bat` if present.
5. Generate a keystore with `Generate Keystore` and keep the keystore file and passwords secure.

---

## Keystore

The `Generate Keystore` button opens a dialog to fill keystore filename, alias, DN fields (CN/OU/O/L/S/C), password(s), key size and validity days. The keystore is created using `keytool` and saved to your `Documents` folder by default.

Security note: passwords are passed to `keytool` on the command line by this quick utility — for production use prefer more secure input and storage workflows.

---

## Notes and Limitations

- The tool intentionally performs a subset of the full list of Android packages you might need. You can expand the package list in `MainWindow.xaml.cs` (the `packages` array) to include additional `cmdline-tools`, `platforms`, `build-tools`, `ndk` versions, and other components.
- Some operations (installer execution, `sdkmanager` changes) may require elevation.
- The app sets environment variables at user scope. Restart terminals or IDEs to pick up changes.
- The `Set JAVA_HOME` dialog sets `JAVA_HOME` to the folder you select; choose the JDK root (for example `C:\\Program Files\\Java\\jdk-21.0.3`).

---

## Files of interest

- `MainWindow.xaml` / `MainWindow.xaml.cs` — main UI and logic.
- `ProgressWindow.xaml` / `ProgressWindow.xaml.cs` — download progress dialog.
- `KeystoreWindow.xaml` / `KeystoreWindow.xaml.cs` — keystore input dialog.
- `KeystoreModel.cs` — keystore parameter model.

---

## Contributing

This is a small local utility. If you want enhancements (more package automation, license acceptance, elevation prompts, embedded logs view), open an issue or submit a PR.

---

## License

This project's source code is licensed under the Apache License, Version 2.0. If you distribute or publish this project, include a copy of the `LICENSE` file with the Apache-2.0 text.

Third-party components and tools referenced or downloaded by this utility are covered by their own licenses. You must comply with those licenses when installing or redistributing those components:

- Android Studio and Android SDK tools: distributed by Google. Review Android Studio and SDK terms at https://developer.android.com/studio/terms
- Android platform, build-tools and NDK: distributed by Google. See Android SDK license details (displayed by `sdkmanager --licenses`).
- Oracle JDK: Oracle provides separate licensing for Oracle JDK downloads; review Oracle Java SE licensing at https://www.oracle.com/java/technologies/javase/jdk-21-licensing.html
- OpenJDK: OpenJDK is distributed under the GNU General Public License, version 2, with the Classpath Exception. See https://openjdk.org/legal/gplv2+ce.html
- Unreal Engine (Epic Games): distributed under Epic Games Unreal Engine EULA. See https://www.unrealengine.com/en-US/eula

This README does not replace the official license terms of third-party products. Use this tool only after you have read and accepted the applicable third-party license terms.
