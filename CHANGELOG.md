# Changelog

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
