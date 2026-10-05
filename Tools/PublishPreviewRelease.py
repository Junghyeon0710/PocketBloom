"""기존 Git 인증으로 현재 origin에 검증된 테스트 배포본을 올린다. 토큰은 출력/저장하지 않는다."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import urllib.error
import urllib.parse
import urllib.request

TAG = "v1.0.0-preview"
FILES = [Path("Builds/Android/PocketBloom-test.apk"), Path("Builds/PocketBloom-Windows.zip"),
         Path("Builds/PocketBloom-StoreKit.zip"), Path("Docs/Media/pocket-bloom-highlight.mp4")]


def git(*args, input_data=None):
    env = dict(os.environ, GIT_TERMINAL_PROMPT="0", GCM_INTERACTIVE="Never")
    result = subprocess.run(["git", "-c", f"safe.directory={Path.cwd().as_posix()}", *args],
                            input=input_data, capture_output=True, text=True, encoding="utf-8", env=env)
    if result.returncode:
        raise RuntimeError(f"Git command failed: {args[0]}")
    return result.stdout.strip()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--publish", action="store_true", help="Create/upload/publish the prerelease; default is a read-only access check.")
    args = parser.parse_args()
    remote = git("remote", "get-url", "origin")
    parsed = urllib.parse.urlparse(remote)
    if parsed.scheme != "https" or parsed.hostname != "github.com" or parsed.username or parsed.password:
        raise RuntimeError("Expected a credential-free HTTPS GitHub origin URL.")
    repo = parsed.path.removeprefix("/").removesuffix(".git")
    if len(repo.split("/")) != 2:
        raise RuntimeError("Unexpected repository path.")
    credential = git("credential", "fill", input_data="protocol=https\nhost=github.com\n\n")
    fields = dict(line.split("=", 1) for line in credential.splitlines() if "=" in line)
    token = fields.get("password")
    if not token:
        raise RuntimeError("Existing Git credentials are unavailable.")
    headers = {"Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
               "X-GitHub-Api-Version": "2026-03-10", "User-Agent": "PocketBloom-delivery"}
    base = f"https://api.github.com/repos/{repo}"

    def api(url, method="GET", data=None, content_type="application/json"):
        body = json.dumps(data, ensure_ascii=False).encode("utf-8") if isinstance(data, dict) else data
        request = urllib.request.Request(url, data=body, method=method, headers=dict(headers, **{"Content-Type": content_type}))
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if error.code == 404 and method == "GET":
                return None
            raise RuntimeError(f"GitHub API returned HTTP {error.code} for {method}.") from None

    info = api(base)
    if not info or not info.get("permissions", {}).get("push"):
        raise RuntimeError("Repository write access was not confirmed.")
    head = git("rev-parse", "HEAD")
    if not args.publish:
        print(json.dumps(dict(repository=repo, write_access=True, default_branch=info["default_branch"], tag=TAG)))
        return
    remote_head = api(base + "/commits/" + urllib.parse.quote(info["default_branch"], safe=""))
    if not remote_head or remote_head["sha"] != head:
        raise RuntimeError("Push and verify the current commit before publishing build attachments.")
    for file in FILES:
        if not file.is_file():
            raise RuntimeError(f"Missing artifact: {file}")
    release = api(base + "/releases/tags/" + TAG)
    if release is None:
        release = api(base + "/releases", "POST", dict(tag_name=TAG, target_commitish=head,
                      name="Pocket Bloom 1.0.0 — 테스트 배포본", draft=True, prerelease=True, make_latest="false",
                      body=Path("Docs/PreviewRelease.ko.md").read_text(encoding="utf-8")))
    elif not release["draft"]:
        raise RuntimeError("This tag already has a published release; choose a new tag rather than replacing it.")
    uploaded = {asset["name"]: asset for asset in release["assets"]}
    evidence = []
    for file in FILES:
        digest = "sha256:" + hashlib.sha256(file.read_bytes()).hexdigest()
        asset = uploaded.get(file.name)
        if asset is None:
            url = release["upload_url"].split("{", 1)[0] + "?" + urllib.parse.urlencode({"name": file.name})
            asset = api(url, "POST", file.read_bytes(), "application/octet-stream")
        if asset.get("digest") != digest or asset.get("size") != file.stat().st_size or asset.get("state") != "uploaded":
            raise RuntimeError(f"Uploaded asset verification failed: {file.name}")
        evidence.append(dict(name=file.name, bytes=asset["size"], digest=asset["digest"], url=asset["browser_download_url"]))
        print(f"Verified upload: {file.name}", flush=True)
    release = api(release["url"], "PATCH", dict(draft=False, prerelease=True, make_latest="false"))
    record = dict(repository=repo, source_commit=head, tag=TAG, url=release["html_url"], assets=evidence)
    # 공개된 소스 커밋 이후의 전달 확인 자료이므로 원본 녹화 폴더에 보존한다.
    path = Path("Recordings/PocketBloom/release-delivery.json")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(record, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(record, ensure_ascii=False))


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, OSError) as error:
        print(str(error))
        raise SystemExit(1)
