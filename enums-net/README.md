# Ivy_EnumsNet_Demo

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy).

Ivy is a web framework for building interactive web applications using C# and .NET.

---

## 📖 Overview

This demo showcases **Enums.NET** usage within an Ivy application. It is designed as an educational sample to help developers understand how to integrate Enums.NET with Ivy's state-driven UI components.

### Features Demonstrated

* **Enumeration modes**: All, Distinct, DisplayOrder, Flags — using `Enums.GetMembers<T>()`.
* **Flags operations** on `DaysOfWeek`: HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags, ToggleFlags.
* **Attributes & Metadata**: Accessing `DescriptionAttribute`, `SymbolAttribute`, `EnumMember`, and `DisplayAttribute`.
* **Parsing & Formatting**: Safe parsing, formatting with `EnumFormat`.
* **Validation**: Using `IsValid()` to validate enum values and flag combinations.

---

## ▶️ Run

```bash
dotnet watch
```

---

## 🚀 Deploy

```bash
ivy deploy
```

---

## 📝 Class Documentation

```
/*
 * Enums.NET Demo App (Ivy)
 *
 * Purpose
 * -------
 * Implements an interactive Ivy view that demonstrates Enums.NET features.
 * Serves as a learning reference for developers.
 *
 * What it demonstrates
 * --------------------
 * 1) Enumeration modes (All, Distinct, DisplayOrder, Flags).
 * 2) Flags operations (HasAllFlags, HasAnyFlags, CombineFlags, CommonFlags, RemoveFlags, GetFlags, ToggleFlags).
 * 3) Helpers (formatting, parsing, validation, attribute inspection).
 *
 * Key types & state
 * -----------------
 * - MembersView enum → controls which member listing is visible.
 * - flagOperations enum → controls which flags operation runs.
 * - EnumMemberInfo record → stores enum metadata (Value, Name, Description, Symbol).
 * - UseState → manages reactive state for UI rendering.
 *
 * Extensibility
 * -------------
 * - Add new enum demos by piping Enums.GetMembers<T>() into EnumMemberInfo.
 * - Add operand selectors (checkboxes) for arbitrary flag combinations.
 * - Extend result rendering with richer layouts (tables, highlights, copy-to-clipboard).
 *
 * Placement
 * ---------
 * - Keep in Ivy sample apps (e.g. EnumsNetApp.Apps) for sidebar visibility.
 *
 * Notes
 * -----
 * - Uses Ivy UI primitives (Text, Card, Button, Layout).
 * - Designed to be extensible, not intrusive.
 */
```

---
