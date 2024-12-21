import subprocess
import re
import json
import os

# 仓库相关信息，这里假设你后续可能会配置这些作为参数传入等，现在先写死示例值
repo = "SweetSmellFox/MFAWPF"

# 获取当前工作目录，后续用于拼接文件路径等
cur_dir = os.getcwd()
contributors_path = os.path.join(cur_dir, "contributors.json")
changelog_path = os.path.join(cur_dir, "../CHANGELOG.md")

# 从哪个tag开始，尝试获取最近的非beta版本标签（更稳定的旧版本标签方式获取）
try:
    result = subprocess.run(["git", "tag", "-l", "v*"], capture_output=True, text=True)
    tags = result.stdout.strip().splitlines()
    stable_tags = [tag for tag in tags if "-" not in tag]
    stable_tags.sort(key=lambda x: [int(i) for i in x[1:].split('.')])
    latest_stable_tag = stable_tags[-1] if stable_tags else None
except subprocess.CalledProcessError as e:
    print(f"获取版本标签出现错误: {e}")
    latest_stable_tag = None

# 当前tag名称，获取最新的匹配v*的标签
try:
    result = subprocess.run(["git", "describe", "--tags", "--match", "v*", "--abbrev=0"], capture_output=True, text=True)
    tag_name = result.stdout.strip()
except subprocess.CalledProcessError as e:
    print(f"获取当前tag名称出现错误: {e}")
    tag_name = None

print(f"From: {latest_stable_tag}, To: {tag_name}")

# 获取详细的git log信息
if latest_stable_tag:
    try:
        git_command = ["git", "log", f"{latest_stable_tag}..HEAD", "--pretty=format:%H%n%aN%n%cN%n%s%n%P"]
        result = subprocess.run(git_command, capture_output=True, text=True)
        raw_gitlogs = result.stdout.strip().splitlines()
    except subprocess.CalledProcessError as e:
        print(f"获取git log信息出现错误: {e}")
        raw_gitlogs = []
else:
    raw_gitlogs = []

raw_commits_info = {}
i = 0
while i < len(raw_gitlogs):
    commit_hash = raw_gitlogs[i]
    i += 1
    author = None
    committer = None
    message = None
    parent = None
    try:
        token = os.environ.get('GH_TOKEN', os.environ.get('GITHUB_TOKEN', None))
        if token:
            url = f"https://api.github.com/repos/{repo}/commits/{commit_hash}"
            headers = {'Authorization': f'Bearer {token}'}
            import requests
            response = requests.get(url, headers=headers)
            data = response.json()
            author = data['author']['login']
            committer = data['committer']['login']
    except (requests.RequestException, KeyError) as e:
        print(f"获取提交 {commit_hash} 的作者或提交者信息出现错误: {e}")

    message = raw_gitlogs[i]
    i += 1
    parent = raw_gitlogs[i].split()
    i += 1
    raw_commits_info[commit_hash] = {
        "hash": commit_hash[:8],
        "author": author,
        "committer": committer,
        "message": message,
        "parent": parent
    }

# 处理Co-authors
coauthors_info = {}
if latest_stable_tag:
    try:
        git_coauthor_command = ["git", "log", f"{latest_stable_tag}..HEAD", "--pretty=format:%H", "--grep=Co-authored-by"]
        result = subprocess.run(git_coauthor_command, capture_output=True, text=True)
        coauthor_hashes = result.stdout.strip().splitlines()
        for commit_hash in coauthor_hashes:
            if commit_hash in raw_commits_info:
                git_addition_command = ["git", "log", commit_hash, "--no-walk", "--pretty=format:%b"]
                result = subprocess.run(git_addition_command, capture_output=True, text=True)
                addition = result.stdout.strip()
                coauthors = []
                coauthor_matches = re.findall(r"Co-authored-by: (.*?) <", addition)
                for coauthor in coauthor_matches:
                    try:
                        token = os.environ.get('GH_TOKEN', os.environ.get('GITHUB_TOKEN', None))
                        if token:
                            url = f"https://api.github.com/repos/{repo}/commits/{commit_hash}"
                            headers = {'Authorization': f'Bearer {token}'}
                            import requests
                            response = requests.get(url, headers=headers)
                            data = response.json()
                            coauthors.append(data['author']['login'])
                    except (requests.RequestException, KeyError) as e:
                        print(f"获取共同作者 {coauthor} 信息出现错误: {e}")
                coauthors_info[commit_hash] = coauthors
    except subprocess.CalledProcessError as e:
        print(f"处理共同作者信息出现错误: {e}")

# 处理跳过的提交
skip_commits_info = {}
if latest_stable_tag:
    try:
        git_skip_command = ["git", "log", f"{latest_stable_tag}..HEAD", "--pretty=format:%H", r"--grep=[skip changelog]"]
        result = subprocess.run(git_skip_command, capture_output=True, text=True)
        skip_hashes = result.stdout.strip().splitlines()
        for commit_hash in skip_hashes:
            if commit_hash in raw_commits_info:
                git_show_command = ["git", "show", "-s", "--format=%b", commit_hash]
                result = subprocess.run(git_show_command, capture_output=True, text=True)
                commit_body = result.stdout.strip()
                if not commit_body.startswith("*") and r"[skip changelog]" in commit_body:
                    skip_commits_info[commit_hash] = True
    except subprocess.CalledProcessError as e:
        print(f"处理跳过的提交信息出现错误: {e}")

# 构建提交树并生成变更日志内容
commit_tree = []


def build_commits_tree(commit_hash):
    global commit_tree
    if commit_hash not in raw_commits_info:
        return
    raw_commit_info = raw_commits_info[commit_hash]
    if "visited" in raw_commit_info and raw_commit_info["visited"]:
        return
    raw_commit_info["visited"] = True
    res = {
        "hash": raw_commit_info["hash"],
        "author": raw_commit_info["author"],
        "committer": raw_commit_info["committer"],
        "message": raw_commit_info["message"],
        "branch": [],
        "skip": False
    }
    if commit_hash in skip_commits_info:
        res["skip"] = True
    parent = raw_commit_info["parent"]
    build_commits_tree(parent[0])
    res["branch"] = commit_tree.copy()
    if len(parent) == 2:
        if re.match(r"^(Release|Merge)", raw_commit_info["message"]):
            build_commits_tree(parent[1])
        else:
            branch_tree = []
            build_commits_tree(parent[1])
            res["branch"] = branch_tree
        if re.match(r"^Merge", raw_commit_info["message"]) and not res["branch"]:
            return
    commit_tree = [res]


if raw_commits_info:
    build_commits_tree(list(raw_commits_info.keys())[0])

sorted_commits = {
    "perf": [],
    "feat": [],
    "fix": [],
    "docs": [],
    "chore": [],
    "other": []
}


def update_commits(commit_message, update_dict):
    oper = "other"
    for trans in ["FIX", "FEAT", "PERF", "DOCS", "CHORE"]:
        if commit_message.startswith(trans):
            oper = trans.lower()
            break
    chinese_keys = ["修复", "新增", "更新", "改进", "优化", "重构", "文档", "杂项", "其他"]
    if oper == "other":
        for key in chinese_keys:
            if key in commit_message:
                if key == "修复":
                    oper = "fix"
                elif key == "新增":
                    oper = "feat"
                elif key == "更新":
                    oper = "perf"
                elif key == "改进":
                    oper = "perf"
                elif key == "优化":
                    oper = "perf"
                elif key == "重构":
                    oper = "perf"
                elif key == "文档":
                    oper = "docs"
                elif key == "杂项":
                    oper = "chore"
                elif key == "其他":
                    oper = "other"
                break
    sorted_commits[oper].append(update_dict)


def print_commits(commits):
    ret_message = ""
    ret_contributor = []
    for commit in commits:
        commit_info = commit
        if commit_info.get("skip", False):
            continue
        commit_message = commit_info["message"]
        if re.match(r"^(build|ci|style|debug)", commit_message):
            continue
        message = commit_message
        if not re.match(r"^(build|chore|ci|docs?|feat!?|fix|perf|refactor|rft|style|test|i18n|typo|debug)", commit_message):
            message = re.sub(r"^(build|chore|ci|docs?|feat!?|fix|perf|refactor|rft|style|test|i18n|typo|debug), *", "", commit_message)
        ret_message += f"* {message}"
        mes = print_commits(commit_info["branch"])
        if not commit_info["branch"]:
            ctrs = []
            for ctr in [commit_info["author"], commit_info.get("coauthors", []), commit_info["committer"]]:
                if ctr and ctr!= "web-flow" and ctr not in ret_contributor:
                    ctrs.append(ctr)
            ret_contributor.extend(ctrs)
        ret_message += f" @{', '.join(ctrs)}" if ctrs else ""
        ret_message += f" ({commit_info['hash']})\n"
        ret_message += mes
    return ret_message


changelog_content = f"## {tag_name}\n{print_commits(commit_tree)}"
try:
    with open(changelog_path, "w") as f:
        f.write(changelog_content)
except IOError as e:
    print(f"写入变更日志文件出现错误: {e}")