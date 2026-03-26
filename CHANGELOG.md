# Changelog

## [0.1.8](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.7...v0.1.8) (2026-03-26)


### Features

* add BasicSample and AnnotatedSample projects under samples/ ([078a17c](https://github.com/awaldow/aspeckd-dotnet/commit/078a17c895d2da8405fd40adac9e6394d142b223))
* add diagnostic steps for version inputs in publish workflow ([34d6ab8](https://github.com/awaldow/aspeckd-dotnet/commit/34d6ab8a05fb50134e71e50af4b86861d8445b8f))
* add NuGet publish workflow with OIDC Trusted Publishing ([c32c7f3](https://github.com/awaldow/aspeckd-dotnet/commit/c32c7f39458c467260982dcda2e7d6fe86c11a3d))
* add package-specific README files and missing NuGet metadata ([aab9f08](https://github.com/awaldow/aspeckd-dotnet/commit/aab9f08892a7d8a7dc1dc1c25cccde28ac4b9509))
* add samples projects ([abad47c](https://github.com/awaldow/aspeckd-dotnet/commit/abad47c679eb273c2c5ab961b04eae6ccde66762))
* use matrix strategy in CI for parallel TFM builds and tests ([042b876](https://github.com/awaldow/aspeckd-dotnet/commit/042b876b88d93f292415b26c5e225954e308a55f))


### Bug Fixes

* add MinVerVersionOverride and version diagnostic step to publish workflow ([0945d5b](https://github.com/awaldow/aspeckd-dotnet/commit/0945d5bdad6501dc82be275e034ad1479f8fda62))
* align sample project TFMs to net8.0;net9.0;net10.0 ([2cf8035](https://github.com/awaldow/aspeckd-dotnet/commit/2cf8035febe12e05501c4b376e581506924f0054))
* attempt 123 at trusted publishing ([d583d12](https://github.com/awaldow/aspeckd-dotnet/commit/d583d12f5c10776fcf98f1bc2b400edaf4206ae8))
* attempt 435 at getting the version to work in publish ([b2a6959](https://github.com/awaldow/aspeckd-dotnet/commit/b2a6959bb342cca612f5c4cd9f84e2cd314bdb60))
* **ci:** derive NuGet package version from release tag ([33b0ed2](https://github.com/awaldow/aspeckd-dotnet/commit/33b0ed26bdf95f529924541b60b58557fb195587))
* **ci:** set pack version from release tag ([2c34353](https://github.com/awaldow/aspeckd-dotnet/commit/2c34353b22d1432a19282b0b8a88605024172bd3))
* **ci:** supply NuGet user and use auth output token for push ([8d95941](https://github.com/awaldow/aspeckd-dotnet/commit/8d95941868a69a8abb72e8899e360b818ca5ea95))
* correct id_token to id-token in publish.yml permissions ([97e77b9](https://github.com/awaldow/aspeckd-dotnet/commit/97e77b9f1d4c441a972bb7322ca64fff9673faea))
* correct OIDC permission key from `id_token` to `id-token` in publish.yml ([44da805](https://github.com/awaldow/aspeckd-dotnet/commit/44da8051b002969ff95f02f7ce807458a21671d6))
* force Version/PackageVersion during pack and add pre-publish guards ([25e7cec](https://github.com/awaldow/aspeckd-dotnet/commit/25e7cecbaacc197a4d5ced5e6ed37eb3f6cbcf4c))
* pass NUGET_USER secret to NuGet/login@v1 user input ([4942784](https://github.com/awaldow/aspeckd-dotnet/commit/4942784876a30a3c77a386b1f98e747a96554d24))
* use NuGet/login@v1 action and rename preview env to prerelease ([8b5f231](https://github.com/awaldow/aspeckd-dotnet/commit/8b5f231ec72e3680146949c100d735c34fffea90))

## [0.1.7](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.6...v0.1.7) (2026-03-26)


### Features

* add diagnostic steps for version inputs in publish workflow ([34d6ab8](https://github.com/awaldow/aspeckd-dotnet/commit/34d6ab8a05fb50134e71e50af4b86861d8445b8f))


### Bug Fixes

* add MinVerVersionOverride and version diagnostic step to publish workflow ([0945d5b](https://github.com/awaldow/aspeckd-dotnet/commit/0945d5bdad6501dc82be275e034ad1479f8fda62))

## [0.1.6](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.5...v0.1.6) (2026-03-26)


### Bug Fixes

* attempt 435 at getting the version to work in publish ([b2a6959](https://github.com/awaldow/aspeckd-dotnet/commit/b2a6959bb342cca612f5c4cd9f84e2cd314bdb60))
* force Version/PackageVersion during pack and add pre-publish guards ([25e7cec](https://github.com/awaldow/aspeckd-dotnet/commit/25e7cecbaacc197a4d5ced5e6ed37eb3f6cbcf4c))

## [0.1.5](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.4...v0.1.5) (2026-03-26)


### Bug Fixes

* attempt 123 at trusted publishing ([d583d12](https://github.com/awaldow/aspeckd-dotnet/commit/d583d12f5c10776fcf98f1bc2b400edaf4206ae8))
* **ci:** supply NuGet user and use auth output token for push ([8d95941](https://github.com/awaldow/aspeckd-dotnet/commit/8d95941868a69a8abb72e8899e360b818ca5ea95))

## [0.1.4](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.3...v0.1.4) (2026-03-26)


### Bug Fixes

* **ci:** derive NuGet package version from release tag ([33b0ed2](https://github.com/awaldow/aspeckd-dotnet/commit/33b0ed26bdf95f529924541b60b58557fb195587))
* **ci:** set pack version from release tag ([2c34353](https://github.com/awaldow/aspeckd-dotnet/commit/2c34353b22d1432a19282b0b8a88605024172bd3))

## [0.1.3](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.2...v0.1.3) (2026-03-26)


### Features

* add BasicSample and AnnotatedSample projects under samples/ ([078a17c](https://github.com/awaldow/aspeckd-dotnet/commit/078a17c895d2da8405fd40adac9e6394d142b223))
* add samples projects ([abad47c](https://github.com/awaldow/aspeckd-dotnet/commit/abad47c679eb273c2c5ab961b04eae6ccde66762))


### Bug Fixes

* align sample project TFMs to net8.0;net9.0;net10.0 ([2cf8035](https://github.com/awaldow/aspeckd-dotnet/commit/2cf8035febe12e05501c4b376e581506924f0054))

## [0.1.2](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.1...v0.1.2) (2026-03-26)


### Features

* add package-specific README files and missing NuGet metadata ([aab9f08](https://github.com/awaldow/aspeckd-dotnet/commit/aab9f08892a7d8a7dc1dc1c25cccde28ac4b9509))

## [0.1.1](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.0...v0.1.1) (2026-03-26)


### Features

* add NuGet publish workflow with OIDC Trusted Publishing ([c32c7f3](https://github.com/awaldow/aspeckd-dotnet/commit/c32c7f39458c467260982dcda2e7d6fe86c11a3d))
* use matrix strategy in CI for parallel TFM builds and tests ([042b876](https://github.com/awaldow/aspeckd-dotnet/commit/042b876b88d93f292415b26c5e225954e308a55f))


### Bug Fixes

* correct id_token to id-token in publish.yml permissions ([97e77b9](https://github.com/awaldow/aspeckd-dotnet/commit/97e77b9f1d4c441a972bb7322ca64fff9673faea))
* correct OIDC permission key from `id_token` to `id-token` in publish.yml ([44da805](https://github.com/awaldow/aspeckd-dotnet/commit/44da8051b002969ff95f02f7ce807458a21671d6))
* pass NUGET_USER secret to NuGet/login@v1 user input ([4942784](https://github.com/awaldow/aspeckd-dotnet/commit/4942784876a30a3c77a386b1f98e747a96554d24))
* use NuGet/login@v1 action and rename preview env to prerelease ([8b5f231](https://github.com/awaldow/aspeckd-dotnet/commit/8b5f231ec72e3680146949c100d735c34fffea90))

## Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!-- Release-please inserts new entries above this line. -->
