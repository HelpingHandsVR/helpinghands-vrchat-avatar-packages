# -*- coding: utf-8 -*-

"""
Packages a Helping Hands VPM package into a UPM .zip file and a .unitypackage

Usually the .unitypackage should not be used, and is mainly intended for allowing users
to refresh a package if it becomes damaged.

:author: scarlet.cafe
:license: MIT
"""

import hashlib
import io
import json
import os
import pathlib
import re
import subprocess
import tarfile
import typing
import zipfile


# Meta configuration
# https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands#grouping-log-lines
GHA_OUTPUT_GROUPS = os.getenv("GHA_OUTPUT_GROUPS", "").lower() in ("1", "true")

# Locate important Unity project folders
DEVOPS_FOLDER = pathlib.Path(__file__).parent
ROOT_FOLDER = DEVOPS_FOLDER.parent
ASSETS_FOLDER = ROOT_FOLDER / 'Assets'
PACKAGES_FOLDER = ROOT_FOLDER / 'Packages'
BUILDS_FOLDER = ROOT_FOLDER / 'Builds'

assert ASSETS_FOLDER.is_dir(), f"Assets folder not found at {ASSETS_FOLDER.absolute()}, something is wrong with the packaging setup"
assert PACKAGES_FOLDER.is_dir(), f"Packages folder not found at {PACKAGES_FOLDER.absolute()}, something is wrong with the packaging setup"
BUILDS_FOLDER.mkdir(exist_ok=True)

# Set up release summary content
RELEASE_SUMMARY_HEADER = """

# \N{WARNING SIGN} Warning: Please use the [VPM repository](https://helpinghandsvr.github.io/vpm/) to install this version of the package.

**Only download the release artifacts below if you know what you're doing.**

## Packages built

""".strip()
RELEASE_SUMMARY_PARTS = [RELEASE_SUMMARY_HEADER]

# Locate UPM package files
if GHA_OUTPUT_GROUPS:
    print("::group::Locating candidate packages")
print("=== Locating candidate packages ===")

UPM_SOURCE_SEARCH_FOLDER = ASSETS_FOLDER / 'HelpingHands'
UPM_PACKAGE_CANDIDATE_PATHS = list(UPM_SOURCE_SEARCH_FOLDER.glob("**/package.json"))

print(f"Found {len(UPM_PACKAGE_CANDIDATE_PATHS)} candidate packages")

class UPMPackageAuthor(typing.TypedDict):
    name: str
    url: str
    email: typing.NotRequired[str]

class UPMPackageManifest(typing.TypedDict):
    name: str
    displayName: str
    version: str
    author: UPMPackageAuthor
    description: typing.NotRequired[str]
    gitDependencies: typing.NotRequired[typing.Dict[str, typing.Any]]
    vpmDependencies: typing.NotRequired[typing.Dict[str, typing.Any]]
    legacyFolders: typing.NotRequired[typing.Dict[str, str]]
    legacyFiles: typing.NotRequired[typing.Dict[str, str]]
    hideInEditor: typing.NotRequired[bool]
    license: typing.NotRequired[str]
    unity: typing.NotRequired[str]

UPM_PACKAGES: typing.List[typing.Tuple[pathlib.Path, UPMPackageManifest]] = []

for upm_package_candidate_path in UPM_PACKAGE_CANDIDATE_PATHS:
    print(f"- {upm_package_candidate_path.relative_to(ROOT_FOLDER)}")
    try:
        with open(upm_package_candidate_path, 'r', encoding='utf-8') as fp:
            package_json_content = json.load(fp)

        name = package_json_content["name"]
        assert isinstance(name, str), f"name field in manifest at {upm_package_candidate_path} was not a string"
        display_name = package_json_content["displayName"]
        assert isinstance(display_name, str), f"displayName field in manifest at {upm_package_candidate_path} was not a string"

        package_manifest = typing.cast(UPMPackageManifest, package_json_content)
        print(f"  ==> [{name}] {display_name}")

        UPM_PACKAGES.append((upm_package_candidate_path, package_manifest))
    except Exception as error:
        print(f"  [!] Failed to read as UPM package: {type(error).__name__}: {error}")

print(f"{len(UPM_PACKAGES)} packages after filtering")
if GHA_OUTPUT_GROUPS:
    print("::endgroup::")

# Collect file content of each package
GUID_REGEX = re.compile(r"\bguid:\s*([0-9a-f]{32})\b")

for (upm_package_manifest_file, upm_package_manifest) in UPM_PACKAGES:
    if GHA_OUTPUT_GROUPS:
        print(f"::group::Generating artifacts for [{upm_package_manifest['name']}] {upm_package_manifest['displayName']}")
    print(f"=== Generating artifacts for [{upm_package_manifest['name']}] {upm_package_manifest['displayName']} ===")

    RELEASE_SUMMARY_PARTS.append(f"### {upm_package_manifest['displayName']}")
    RELEASE_SUMMARY_PARTS.append(f"ID: `{upm_package_manifest['name']}`\n")
    RELEASE_SUMMARY_PARTS.append(f"Version: {upm_package_manifest['version']}\n")

    RELEASE_SUMMARY_PARTS.append("| File | SHA256 |")
    RELEASE_SUMMARY_PARTS.append("| -- | -- |")

    # Collect files and folders
    upm_package_root = upm_package_manifest_file.parent
    upm_target_folder = PACKAGES_FOLDER / upm_package_manifest['name']
    upm_folders: typing.List[typing.Tuple[pathlib.Path, str, str]] = []
    upm_files: typing.List[typing.Tuple[pathlib.Path, str, bytes, str]] = []

    folders_considered: int = 0
    folders_lacking_meta: int = 0
    folders_lacking_guid: int = 0
    files_considered: int = 0
    files_lacking_meta: int = 0
    files_lacking_guid: int = 0

    for root, dirs, files in upm_package_root.walk():
        # Add folders
        for dir in dirs:
            folders_considered += 1
            dir_location = root / dir
            dir_meta_location = dir_location.with_name(dir_location.name + ".meta")

            if not dir_meta_location.is_file():
                # We don't care about directories that don't have matching meta files
                folders_lacking_meta += 1
                continue

            with open(dir_meta_location, 'r', encoding='utf-8') as fp:
                dir_meta = fp.read()

            dir_guid = GUID_REGEX.search(dir_meta)

            if not dir_guid:
                folders_lacking_guid += 1
                continue

            upm_folders.append((dir_location.relative_to(upm_package_root), dir_guid.group(1), dir_meta))

        # Add files
        for file in files:
            file_location = root / file

            if file_location.name.lower().endswith(".meta"):
                # We don't care about meta files because we'll discover them alongside their associated file
                # This also effectively filters out dangling meta files
                continue

            files_considered += 1

            file_meta_location = file_location.with_name(file_location.name + ".meta")

            if not file_meta_location.is_file():
                # We don't care about files that lack a meta file
                # This is usually a mistake, but it means we can't package properly so it's better to ignore
                files_lacking_meta += 1
                continue

            with open(file_meta_location, 'r', encoding='utf-8') as fp:
                file_meta = fp.read()

            file_guid = GUID_REGEX.search(file_meta)

            if not file_guid:
                files_lacking_guid += 1
                continue

            with open(file_location, 'rb') as fp:
                file_content = fp.read()

            upm_files.append((file_location.relative_to(upm_package_root), file_guid.group(1), file_content, file_meta))

    # Collect all file and folder names
    upm_entities = set(x[0] for x in upm_files) | set(x[0] for x in upm_folders)

    # Check if any of them are git-ignored
    git_check_ignore = subprocess.run(
        [
            "git",
            "check-ignore",
            *[
                (upm_package_root / path).relative_to(ROOT_FOLDER).as_posix()
                for path in upm_entities
            ]
        ],
        cwd=ROOT_FOLDER, encoding='utf-8', capture_output=True
    )
    git_ignored_entities = [
        x.strip() for x in
        git_check_ignore.stdout.replace("\r\n", "\n").strip().split("\n")
        if x.strip()
    ]

    file_count_before = len(upm_files)
    folder_count_before = len(upm_folders)

    upm_files = [
        file for file in upm_files
        if (upm_package_root / file[0]).relative_to(ROOT_FOLDER).as_posix() not in git_ignored_entities
    ]

    upm_folders = [
        folder for folder in upm_folders
        if (upm_package_root / folder[0]).relative_to(ROOT_FOLDER).as_posix() not in git_ignored_entities
    ]

    files_gitignored: int = file_count_before - len(upm_files)
    folders_gitignored: int = folder_count_before - len(upm_folders)

    # Filtering complete, now list what is being included
    entities_to_print = [
        (True, file[0]) for file in upm_files
    ] + [
        (False, folder[0]) for folder in upm_folders
    ]
    entities_to_print.sort(key=lambda p: p[1])

    for entity in entities_to_print:
        blank_space = len(entity[1].parent.as_posix())
        print("  " + (" " * blank_space) + entity[1].name + ("" if entity[0] else "/"))

    print(f"{files_considered} files considered, {len(upm_files)} files included, {files_lacking_meta} no meta, {files_lacking_guid} no guid, {files_gitignored} gitignored")
    print(f"{folders_considered} folders considered, {len(upm_folders)} folders included, {folders_lacking_meta} no meta, {folders_lacking_guid} no guid, {folders_gitignored} gitignored")

    # Create output artifacts
    zip_output = BUILDS_FOLDER / f"{upm_package_manifest['name']}.v{upm_package_manifest['version']}.zip"
    with zipfile.ZipFile(zip_output, mode='w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as upm_zip:
        for path, _guid, meta in upm_folders:
            upm_zip.writestr(path.with_name(path.name + ".meta").as_posix(), meta.encode('utf-8'))

        for path, _guid, data, meta in upm_files:
            upm_zip.writestr(path.as_posix(), data)
            upm_zip.writestr(path.with_name(path.name + ".meta").as_posix(), meta.encode('utf-8'))

    unitypackage_output = BUILDS_FOLDER / f"{upm_package_manifest['name']}.v{upm_package_manifest['version']}.unitypackage"
    with open(unitypackage_output, 'wb') as unitypackage_inner:
        with tarfile.open(name='archtemp.tar', fileobj=unitypackage_inner, mode='w:gz', compresslevel=9) as unitypackage:

            for path, guid, meta in upm_folders:
                for name, content in (
                    ('asset.meta', meta.encode('utf-8')),
                    ('pathname', (upm_target_folder / path).relative_to(ROOT_FOLDER).as_posix().encode('utf-8'))
                ):
                    info = tarfile.TarInfo(name=f"{guid}/{name}")
                    info.size = len(content)

                    unitypackage.addfile(info, io.BytesIO(content))

            for path, guid, data, meta in upm_files:
                for name, content in (
                    ('asset', data),
                    ('asset.meta', meta.encode('utf-8')),
                    ('pathname', (upm_target_folder / path).relative_to(ROOT_FOLDER).as_posix().encode('utf-8'))
                ):
                    info = tarfile.TarInfo(name=f"{guid}/{name}")
                    info.size = len(content)

                    unitypackage.addfile(info, io.BytesIO(content))

    # Add info to release manifest
    artifacts = [zip_output, unitypackage_output]

    for file in artifacts:
        with open(file, 'rb') as fp:
            sha256 = hashlib.sha256(fp.read()).hexdigest()

        RELEASE_SUMMARY_PARTS.append(f"| {file.name} | `{sha256}` |")

    if GHA_OUTPUT_GROUPS:
        print("::endgroup::")

# Produce release summary
if GHA_OUTPUT_GROUPS:
    print("::group::Producing release manifest")
print("=== Producing release manifest ===")

RELEASE_SUMMARY = "\n".join(RELEASE_SUMMARY_PARTS)
print(RELEASE_SUMMARY)
with open(BUILDS_FOLDER / "RELEASE_SUMMARY.md", 'w', encoding='utf-8') as fp:
    fp.write(RELEASE_SUMMARY + "\n")

if GHA_OUTPUT_GROUPS:
    print("::endgroup::")
