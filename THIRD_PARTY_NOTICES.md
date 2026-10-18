# Third-party components

The checked-in Gradle 8.9 wrapper JAR and launcher scripts come from the official [Gradle v8.9.0 repository](https://github.com/gradle/gradle/tree/v8.9.0). They retain their upstream notices and are redistributed under Apache License 2.0; see `LICENSES/Gradle-Apache-2.0.txt`.

Verified wrapper JAR SHA-256: `498495120a03b9a6ab5d155f5de3c8f0d986a449153702fb80fc80e134484f17`, matching the official Gradle checksum endpoint. The Gradle distribution checksum is pinned separately in `android/gradle/wrapper/gradle-wrapper.properties`.

Android builds resolve AndroidX Core 1.13.1 and its transitive dependencies from Google's Maven repository. AndroidX uses Apache-2.0 licenses; build dependencies may have their own notices. Release distribution should retain applicable dependency notices and inventory the final artifacts.

The self-contained Windows build bundles the Microsoft .NET/Windows Desktop runtime and Windows SDK projections under their respective licenses/notices. These components are build outputs, not original application source. See the .NET [third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT) and [license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT). Keep vendor-provided license/notice files when packaging runtime distributions.

Original project source has no selected public license yet. Choose one before publishing as an open-source project. This does not override any upstream component license.
