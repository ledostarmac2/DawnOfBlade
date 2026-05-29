#!/usr/bin/env python3
"""Dawn of Blade agent task board.

A tiny, dependency-free coordination layer so multiple coding agents (e.g.
Claude and Codex) and humans can claim, hand off, and complete tasks via a
shared JSON file. No networking, no APIs, no background services.

Usage:
    python tools/agentboard.py list [--state STATE] [--owner AGENT]
    python tools/agentboard.py show LQ-001
    python tools/agentboard.py add  --id LQ-010 --title "..." [--notes "..."] [--depends LQ-001,LQ-002]
    python tools/agentboard.py claim    LQ-001 --owner claude
    python tools/agentboard.py delegate LQ-001 --to codex [--note "..."]
    python tools/agentboard.py state    LQ-001 --to review [--note "..."] [--verify "dotnet build ok"]
    python tools/agentboard.py touch    LQ-001 --files src/Foo.cs,src/Bar.cs

The board file defaults to tools/agent-board.json. Set AGENT_BOARD to override
(e.g. a gitignored tools/agent-board.local.json for private WIP notes).
"""
from __future__ import annotations

import argparse
import datetime as _dt
import json
import os
import sys

DEFAULT_BOARD = os.path.join(os.path.dirname(__file__), "agent-board.json")
BOARD_PATH = os.environ.get("AGENT_BOARD", DEFAULT_BOARD)
STATES = ["open", "claimed", "blocked", "review", "done"]


def _today():
    return _dt.date.today().isoformat()


def load():
    with open(BOARD_PATH, "r", encoding="utf-8") as fh:
        return json.load(fh)


def save(board):
    with open(BOARD_PATH, "w", encoding="utf-8") as fh:
        json.dump(board, fh, indent=2)
        fh.write("\n")


def find(board, task_id):
    for task in board["tasks"]:
        if task["id"] == task_id:
            return task
    sys.exit("error: task %r not found in %s" % (task_id, BOARD_PATH))


def _split(value):
    if not value:
        return []
    return [part.strip() for part in value.split(",") if part.strip()]


def cmd_list(board, args):
    rows = board["tasks"]
    if args.state:
        rows = [t for t in rows if t.get("state") == args.state]
    if args.owner:
        rows = [t for t in rows if t.get("owner") == args.owner]
    if not rows:
        print("(no matching tasks)")
        return
    for t in rows:
        owner = t.get("owner") or "-"
        dep = ",".join(t.get("depends") or []) or "-"
        print("%-8s %-8s owner=%-8s deps=%-14s %s" % (
            t["id"], t.get("state", ""), owner, dep, t.get("title", "")))


def cmd_show(board, args):
    print(json.dumps(find(board, args.id), indent=2))


def cmd_add(board, args):
    if any(t["id"] == args.id for t in board["tasks"]):
        sys.exit("error: task %r already exists" % args.id)
    board["tasks"].append({
        "id": args.id,
        "title": args.title,
        "state": "open",
        "owner": None,
        "delegateTo": None,
        "depends": _split(args.depends),
        "filesTouched": [],
        "notes": args.notes or "",
        "verification": [],
        "updated": _today(),
    })
    save(board)
    print("added " + args.id)


def cmd_claim(board, args):
    task = find(board, args.id)
    task["owner"] = args.owner
    task["delegateTo"] = None
    task["state"] = "claimed"
    task["updated"] = _today()
    save(board)
    print("%s claimed by %s" % (args.id, args.owner))


def cmd_delegate(board, args):
    task = find(board, args.id)
    task["delegateTo"] = args.to
    task["state"] = "open"
    task["owner"] = None
    if args.note:
        task["notes"] = args.note
    task["updated"] = _today()
    save(board)
    print("%s delegated to %s" % (args.id, args.to))


def cmd_state(board, args):
    if args.to not in STATES:
        sys.exit("error: state must be one of %s" % STATES)
    task = find(board, args.id)
    task["state"] = args.to
    if args.note:
        task["notes"] = args.note
    if args.verify:
        task.setdefault("verification", []).append(args.verify)
    task["updated"] = _today()
    save(board)
    print("%s -> %s" % (args.id, args.to))


def cmd_touch(board, args):
    task = find(board, args.id)
    files = set(task.get("filesTouched") or []) | set(_split(args.files))
    task["filesTouched"] = sorted(files)
    task["updated"] = _today()
    save(board)
    print("%s files: %s" % (args.id, ", ".join(task["filesTouched"])))


def build_parser():
    p = argparse.ArgumentParser(description="Dawn of Blade agent task board")
    sub = p.add_subparsers(dest="command", required=True)

    s = sub.add_parser("list", help="list tasks")
    s.add_argument("--state", choices=STATES)
    s.add_argument("--owner")
    s.set_defaults(func=cmd_list)

    s = sub.add_parser("show", help="show one task as JSON")
    s.add_argument("id")
    s.set_defaults(func=cmd_show)

    s = sub.add_parser("add", help="add a new task")
    s.add_argument("--id", required=True)
    s.add_argument("--title", required=True)
    s.add_argument("--notes")
    s.add_argument("--depends")
    s.set_defaults(func=cmd_add)

    s = sub.add_parser("claim", help="claim a task")
    s.add_argument("id")
    s.add_argument("--owner", required=True)
    s.set_defaults(func=cmd_claim)

    s = sub.add_parser("delegate", help="hand a task to another agent")
    s.add_argument("id")
    s.add_argument("--to", required=True)
    s.add_argument("--note")
    s.set_defaults(func=cmd_delegate)

    s = sub.add_parser("state", help="change task state")
    s.add_argument("id")
    s.add_argument("--to", required=True)
    s.add_argument("--note")
    s.add_argument("--verify")
    s.set_defaults(func=cmd_state)

    s = sub.add_parser("touch", help="record files touched")
    s.add_argument("id")
    s.add_argument("--files", required=True)
    s.set_defaults(func=cmd_touch)
    return p


def main(argv):
    args = build_parser().parse_args(argv)
    board = load()
    args.func(board, args)


if __name__ == "__main__":
    main(sys.argv[1:])
