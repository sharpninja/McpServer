@echo off
pwsh -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0aiunit-grok-evaluator.ps1" %*
