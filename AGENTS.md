# AGENTS.md

## 1. Language Policy
- **Repository & Codebase**: Everything stored in this repository must be written in **English**. This includes source code, identifiers, comments, docstrings, log/UI strings, documentation, Markdown files, and commit messages.
- **Agent Conversation**: Always communicate with the user in the **user's language** (for example, respond in Chinese when the user writes in Chinese).

## 2. Package Overview
- **Package Name**: `com.turingcat.lumiscore` (`Lumiscore`)
- **Description**: A Timeline-first stage lighting authoring toolkit for Unity and VRChat.
- **Target Unity Version**: `2022.3`
- **Structure**:
  - `Runtime/` — Runtime components and assets.
  - `Editor/` — Unity Editor tooling and authoring UI.

## 3. Unity Package Guidelines
- Keep Unity `.meta` files under version control together with their corresponding files and directories so asset/script GUIDs remain stable.
