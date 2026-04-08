# Changelog

## [0.1.9](https://github.com/awaldow/aspeckd-dotnet/compare/v0.1.8...v0.1.9) (2026-04-08)


### Features

* add AgentToolGroupAttribute for grouping API operations ([e851c88](https://github.com/awaldow/aspeckd-dotnet/commit/e851c8890e9cf97fd631c21a623ac7892655b517))
* Add auth representation to agent spec (AgentAuthInfo model, root/group auth blocks) ([9b33039](https://github.com/awaldow/aspeckd-dotnet/commit/9b33039b1451e4db900ac59ea3af26a35cd350c3))
* Auth representation in the agent spec doc tree ([a211cfe](https://github.com/awaldow/aspeckd-dotnet/commit/a211cfe13c8de11a6f328c48642c2811f8c0cc7d))
* Build-time description quality warnings (ASPECKD001/ASPECKD002) ([eea2b1c](https://github.com/awaldow/aspeckd-dotnet/commit/eea2b1cfc1675e5f5c424e0ea8e42016687b0389))
* build-time description warnings (ASPECKD001/ASPECKD002) ([da43d38](https://github.com/awaldow/aspeckd-dotnet/commit/da43d385c3398f07066cf0db2e83437fe27677b7))
* build-time static agent spec generation ([14ef43d](https://github.com/awaldow/aspeckd-dotnet/commit/14ef43d7430e88e031f80500082e90a89389afe9))
* build-time static generation of /.well-known/agents/* doc tree ([dc0dceb](https://github.com/awaldow/aspeckd-dotnet/commit/dc0dceba9cce1548552399676cee4e33086fe2dd))
* endpoint-level required claims + /.well-known/agents default path ([e37ce50](https://github.com/awaldow/aspeckd-dotnet/commit/e37ce50c46348b6e870ed8958d911eb013b25d2c))
* replace golden spec with comprehensive factory exercising all features ([baac987](https://github.com/awaldow/aspeckd-dotnet/commit/baac98712fce679a38a78ed1ba735b239315127a))
* slim index.json to name/description/detailUrl only ([59ac036](https://github.com/awaldow/aspeckd-dotnet/commit/59ac036e1b3876c995773371f2bb5b7ec49950b5))


### Bug Fixes

* correct test method name spelling ([2a8aafe](https://github.com/awaldow/aspeckd-dotnet/commit/2a8aafef1f9d8b8b4b9add3cb16ecf5a7c78e904))
* prevent path traversal in StaticFileAgentSpecProvider ([6357827](https://github.com/awaldow/aspeckd-dotnet/commit/63578270550a23b77d194c1326211a8bc1748ed6))
* remove redundant null-coalesce in AgentRequiredClaimsAttribute; drop trailing period from golden spec description ([da8eef2](https://github.com/awaldow/aspeckd-dotnet/commit/da8eef207f91a14bcc684298ead93618cad2c3f4))
* Remove unused constant and redundant null assignments in test helpers ([d1f9ff9](https://github.com/awaldow/aspeckd-dotnet/commit/d1f9ff98b6e7f58a50a101d4f667ad220d9c04d7))

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
