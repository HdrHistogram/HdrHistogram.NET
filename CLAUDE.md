# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HdrHistogram.NET is the official port of the Java HdrHistogram library.
HdrHistogram supports the recording and analyzing of sampled data value counts across a configurable integer value range with configurable value precision within the range.
Value precision is expressed as the number of significant digits in the value recording, and provides control over value quantization behavior across the value range and the subsequent value resolution at any given level.

HdrHistogram aims to be an extremely fast, low resource usage tool to collect large amounts of insturmentation data (response times). 

## Markdown standards

- Use British English.
- One sentence per line.
- One line per sentence.
- Headings have blank line under them
- Ordered and unordered lists have a blank line before and after them

## Git Workflow

- **Never commit directly to `main`** — pre-commit and pre-push hooks enforce this
- Create feature branches: `git checkout -b feat/<description>`
- Push feature branches and create PRs against `main`. If running from a fork, target upstream main.
- Branch naming convention: `feat/`, `fix/`, `chore/` prefixes
- PRs should target only one issue. An Issue may have multiple PRs to solve it in a managable way.
- Before creating PRs, always have other agents test and review the code.
