# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-08-12

### Fixed

- **Documents were downloaded as their first revision instead of the latest one.**
  `DownloadDocument` already queried the latest revision to build the file name, but
  the download itself was hard-coded to revision `1`, so every migrated document
  contained outdated content whenever it had more than one revision. The revision
  number from `GetLatestRevision` is now passed through to the download
  ([ApiConnection.cs](ApiConnection.cs)). Emails still use revision `1` — they only
  ever have a single revision.
- **Retry after a network error deleted the wrong path.** On `WebException` /
  `IOException` the retry logic called `File.Delete(directory)` on the target
  *directory* rather than on the partially written file, so the incomplete file was
  left in place. Because the retry then saw an existing file, it logged
  "already exist" and skipped the download entirely, silently leaving a truncated
  file behind. The partial file is now deleted before retrying
  ([ApiConnection.cs](ApiConnection.cs)).
- **Crash on items with an empty `FileAs`.** Documents and emails whose `FileAs`
  field was `null` threw a `NullReferenceException` and aborted the migration. Such
  items now fall back to the name `NO_NAME` ([ApiConnection.cs](ApiConnection.cs)).
- **Crash when trimming long names containing a dot.** `TrimPathLength` treated
  everything after the last dot as a file extension. An email subject such as
  `Re: meeting notes. Please read the whole attached summary before Monday` produced
  an "extension" longer than the allowed length, making the `Substring` call throw
  `ArgumentOutOfRangeException`. When the detected extension does not fit into the
  length budget it is now dropped and the name is trimmed plainly
  ([FileHelper.cs](FileHelper.cs)).
- **Invalid characters in email file names.** The trimmed email name is now sanitized
  with `RemoveInvalidFileNameChars` before the GUID suffix is appended, so subjects
  containing characters such as `:`, `/` or `?` no longer produce invalid paths
  ([ApiConnection.cs](ApiConnection.cs)).

### Changed

- Updated `log4net` from 2.0.10 to 3.3.0 ([MigrateDocuments.csproj](MigrateDocuments.csproj)).
- Bumped assembly version to 1.1.0.0 ([Properties/AssemblyInfo.cs](Properties/AssemblyInfo.cs)).

## [1.0.0]

Initial version.

### Added

- Migration of contact, company and project documents out of eWay-CRM into a local
  folder structure.
- Migration of emails attached to the migrated items.
- Retry with a 15 second delay on network errors during download.
- File and folder names carry the first part of the item GUID to avoid false
  duplicates, are trimmed to stay within the Windows `MAX_PATH` limit, and are
  stripped of characters that are invalid on the target platform.
