# Agent maintenance scripts

Run these utilities from `agentic-service` as modules, for example:

```powershell
python -m scripts.sample_layout
python -m scripts.verify_catalog
python -m scripts.verify_details
```

Automated tests belong in `tests/`. Temporary debugging and source-rewriting
scripts should not be committed to the repository root.
