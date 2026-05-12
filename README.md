# ProjectScanner (`pscanner`)

A global .NET CLI tool that scans a project directory, detects the tech stack, and generates a step-by-step report on how to run it — including terminal commands, port numbers, and environment setup.

Built for developers who have too many projects and can't remember how to run any of them.

---

## Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Build and install globally

```bash
git clone https://github.com/YOURUSERNAME/ProjectScanner.git
cd ProjectScanner
dotnet pack
dotnet tool install --global --add-source ./bin/Release pscanner
```

Once installed, `pscanner` is available in any terminal window on your machine.

---

## Usage

**Scan the current directory:**
```bash
pscanner
```

**Scan a specific project folder:**
```bash
pscanner C:\Users\you\Projects\MyApp
```

**Paths with spaces — no quotes needed:**
```bash
pscanner C:\Users\you\Projects\My App
```

---

## Output

Running `pscanner` produces two things:

1. A report printed directly to the console
2. A `HOW_TO_RUN.txt` file saved inside the scanned project folder

### Example output

```
Scanning project at: C:\Projects\Blog Platform
--------------------------------------------------
Stack Detected : Node.js / JavaScript + Java / Maven
How to run this project:
==================================================
  Step 1: Open a terminal in the project folder
  Step 2: Run: npm install
  Step 3: Run: npm run dev
  Step 4: Run: npm run build
  Step 5: Run: mvn install
  Step 6: Run: mvn spring-boot:run
  Step 7: Docker detected — alternatively run: docker-compose up
Requires 3 terminal(s):
  Terminal 1: npm run dev
  Terminal 2: mvn spring-boot:run
  Terminal 3: docker-compose up
Ports:
  http://localhost:5173  (Vite default)
Report saved to: C:\Projects\Blog Platform\HOW_TO_RUN.txt
```

---

## Supported Stacks

| Stack | Detected By |
|---|---|
| Node.js / JavaScript | `package.json` |
| React / Vue / Vite | `vite.config.js` or `vite.config.ts` |
| ASP.NET Core / .NET | `*.csproj` |
| Visual Studio Solution | `*.sln` |
| Python | `requirements.txt` |
| Python / Django | `requirements.txt` + `manage.py` |
| Ruby | `Gemfile` |
| Java / Maven | `pom.xml` |
| Java / Gradle | `build.gradle` |
| C++ / CMake | `CMakeLists.txt` |
| Go | `go.mod` |
| Rust | `Cargo.toml` |
| PHP / Composer | `composer.json` |
| Docker | `docker-compose.yml` |

Multiple stacks are detected simultaneously for fullstack projects.

---

## Port Detection

`pscanner` attempts to detect the port the app runs on from:

- `.env` — looks for `PORT=XXXX`
- `vite.config.js` / `vite.config.ts` — looks for `port:` in the config
- `launchSettings.json` — looks for `applicationUrl` (.NET projects)

If no port is found, framework defaults are used (e.g. Vite defaults to `5173`).

---

## Environment File Detection

- If `.env.example` exists but `.env` does not — warns you to copy and fill it in
- If `.env` exists — reads it for port configuration

---

## Updating

After making code changes, reinstall with:

```bash
dotnet pack
dotnet tool uninstall --global pscanner
dotnet tool install --global --add-source ./bin/Release pscanner
```

---


