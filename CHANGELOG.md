# Changelog

## [1.7.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.6.2...v1.7.0) (2025-08-13)


### Features

* adds language support to AI jobs & fix: paging get user folder, response folder ([#527](https://github.com/tranduckhuy/eduva-backend/issues/527)) ([64af27f](https://github.com/tranduckhuy/eduva-backend/commit/64af27fd61b470ca024650b803fe9bcd9a0052e4))

## [1.6.2](https://github.com/tranduckhuy/eduva-backend/compare/v1.6.1...v1.6.2) (2025-08-12)


### Bug Fixes

* only count LM has status is Active & IsPagingEnabled get all class ([#523](https://github.com/tranduckhuy/eduva-backend/issues/523)) ([20ed914](https://github.com/tranduckhuy/eduva-backend/commit/20ed9140593e4425aac04b60f30d80e2af3a2380))

## [1.6.1](https://github.com/tranduckhuy/eduva-backend/compare/v1.6.0...v1.6.1) (2025-08-11)


### Bug Fixes

* **auth:** add missing SegmentsPerWindow setting in rate limiting config ([#519](https://github.com/tranduckhuy/eduva-backend/issues/519)) ([5da0110](https://github.com/tranduckhuy/eduva-backend/commit/5da01101413c30d6cf93cb39e32ca45bbd7c44fe))

## [1.6.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.5.4...v1.6.0) (2025-08-11)


### Features

* Implements subscription maintenance service  ([#516](https://github.com/tranduckhuy/eduva-backend/issues/516)) ([51fefe4](https://github.com/tranduckhuy/eduva-backend/commit/51fefe4619c6c30ea2b2f1faa5d26ce118a0d6c7))

## [1.5.4](https://github.com/tranduckhuy/eduva-backend/compare/v1.5.3...v1.5.4) (2025-08-08)


### Bug Fixes

* support sorting by lastmodifiedat, createdat, enrolledat in class ([#513](https://github.com/tranduckhuy/eduva-backend/issues/513)) ([7418d00](https://github.com/tranduckhuy/eduva-backend/commit/7418d002c58e9684a4f373870f251bea64d39f8a))

## [1.5.3](https://github.com/tranduckhuy/eduva-backend/compare/v1.5.2...v1.5.3) (2025-08-06)


### Bug Fixes

* **ai-job:** notification still sends default FailureReason ([#510](https://github.com/tranduckhuy/eduva-backend/issues/510)) ([361adb8](https://github.com/tranduckhuy/eduva-backend/commit/361adb84a3be94208f5ec8893c14a7c19e5d2c06))

## [1.5.2](https://github.com/tranduckhuy/eduva-backend/compare/v1.5.1...v1.5.2) (2025-08-06)


### Bug Fixes

* **ai-job:** limit uploaded file size to 20MB when creating job ([#508](https://github.com/tranduckhuy/eduva-backend/issues/508)) ([035e36f](https://github.com/tranduckhuy/eduva-backend/commit/035e36f0bc5a91adbf2f078d3eed85c73626d780))

## [1.5.1](https://github.com/tranduckhuy/eduva-backend/compare/v1.5.0...v1.5.1) (2025-08-06)


### Bug Fixes

* Do not allow teachers to perform anything on the class Archived and fix restore folder & enhance(ai-job): add title to notification response ([f0fa5b3](https://github.com/tranduckhuy/eduva-backend/commit/f0fa5b3af8afcba3b132272e5f64f9faadd14239))

## [1.5.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.4.2...v1.5.0) (2025-08-05)


### Features

* Enhances job processing and file handling & only change status class and folder (archived) when archive class ([#503](https://github.com/tranduckhuy/eduva-backend/issues/503)) ([44364e5](https://github.com/tranduckhuy/eduva-backend/commit/44364e5b336bed5228345604f237df60bf989167))

## [1.4.2](https://github.com/tranduckhuy/eduva-backend/compare/v1.4.1...v1.4.2) (2025-08-04)


### Bug Fixes

* handle archive class will also archive folders and delete lesson materials ([#494](https://github.com/tranduckhuy/eduva-backend/issues/494)) ([e168ddd](https://github.com/tranduckhuy/eduva-backend/commit/e168dddfd7c427072f18b14f347d565a1ee1fd90))

## [1.4.1](https://github.com/tranduckhuy/eduva-backend/compare/v1.4.0...v1.4.1) (2025-08-02)


### Bug Fixes

* database connection transient failure and configures logging & add lesson material shared in school into person class & refactor: localize worksheet name and exclude SchoolAdmin from notifications ([#484](https://github.com/tranduckhuy/eduva-backend/issues/484))  ([5772fd7](https://github.com/tranduckhuy/eduva-backend/commit/5772fd70c2f12ee101b8518665de92063765dcad))

## [1.4.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.3.0...v1.4.0) (2025-08-01)


### Features

* add rate limiting for ai creation & update database required userId when create folder ([35f0e02](https://github.com/tranduckhuy/eduva-backend/commit/35f0e021a42c95c6ee61b125f05a2a037ef27fec))
* permanently delete all soft-deleted lesson materials ([#469](https://github.com/tranduckhuy/eduva-backend/issues/469)) ([c28a53f](https://github.com/tranduckhuy/eduva-backend/commit/c28a53fe3968cd923e9a0b43556f6430f2e82812))

## [1.3.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.2.0...v1.3.0) (2025-07-27)


### Features

* implement rate limiting and cooldown for email-related actions & fix notification service to send correct userNotificationId ([#428](https://github.com/tranduckhuy/eduva-backend/issues/428)) ([b042da5](https://github.com/tranduckhuy/eduva-backend/commit/b042da5392315af8f9cfcb1c0f6bd5d0371a21d3))

## [1.2.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.1.0...v1.2.0) (2025-07-25)


### Features

* improve lesson material deletion & fix access control issues ([#403](https://github.com/tranduckhuy/eduva-backend/issues/403)) ([83e7e4e](https://github.com/tranduckhuy/eduva-backend/commit/83e7e4e6968c0bbbd4dca6d3b1074f388a49af7c))

## [1.1.0](https://github.com/tranduckhuy/eduva-backend/compare/v1.0.0...v1.1.0) (2025-07-25)


### Features

* **ai generator:** enables job updates for users. ([53b4f31](https://github.com/tranduckhuy/eduva-backend/commit/53b4f31ea7386fe9e0f6e7d2690aaf2c6969c0f1))

## 1.0.0 (2025-07-24)


### Features
🚀 Eduva Backend v1.0.0 – Initial Release
We’re excited to launch the first official version of the Eduva backend!
Stay tuned for future updates and improvements!
