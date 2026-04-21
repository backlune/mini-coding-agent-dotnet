&nbsp;
# Interactive Example

Hands-on workflow for using the .NET `mini-coding-agent` against a tiny
Python project. The agent itself is C#, but it happily drives any language
— this example just happens to generate Python.

The flow is:

1. create a fresh repo
2. launch the agent against it
3. implement `binary_search.py`
4. edit the implementation
5. add `pytest` tests
6. run tests
7. fix anything that fails

This example assumes:

- `ollama serve` is already running
- you already pulled a model such as `qwen3.5:4b` (e.g., via `ollama pull qwen3.5:4b`)
- you cloned this repository and ran `dotnet build` once

If you have sufficient memory, consider using a larger Qwen 3.5 model instead:

- [ollama.com/library/qwen3.5](https://ollama.com/library/qwen3.5)

&nbsp;
## 1. Create a fresh repo

```bash
mkdir -p ./tmp/binary-search-repo
cd ./tmp/binary-search-repo
git init
```

At this point the repo is basically empty:

```bash
ls -la
```

&nbsp;
## 2. Launch the agent

From the `mini-coding-agent` clone, but point it at the new repo:

```bash
dotnet run --project src/MiniCodingAgent -- \
  --cwd ./tmp/binary-search-repo \
  --model "qwen3.5:4b"
```

&nbsp;
## 3. Ask it to implement binary search

At the `mini-coding-agent>` prompt, paste:

```text
Inspect this repository and create a minimal binary_search.py file. Implement an iterative binary_search(nums, target) function for a sorted list of integers. Return the index if the target exists and -1 if it does not. Keep the code very small.
```

&nbsp;
## 4. Ask it to edit the implementation

```text
Update binary_search.py so it raises ValueError if the input list is not sorted in ascending order. Keep the implementation simple.
```

&nbsp;
## 5. Ask it to add unit tests

```text
Create test_binary_search.py with pytest tests for found, missing, empty list, first element, last element, and unsorted input raising ValueError. Keep the tests small and readable.
```

&nbsp;
## 6. Ask it to run the tests

```text
Run pytest for this repo. If any test fails, fix the code or tests and rerun until everything passes.
```

&nbsp;
## 7. Inspect the final repo state

```bash
cd tmp/binary-search-repo
git status --short
```

You should now have:

- `binary_search.py`
- `test_binary_search.py`

&nbsp;
## 8. Useful interactive commands

- `/help` shows the available slash commands
- `/memory` prints the agent's distilled working memory
- `/session` shows the path to the saved session JSON file
- `/reset` clears the current conversation history and working memory
- `/exit` leaves the interactive agent
