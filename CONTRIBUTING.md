# Contributing to ShepherdProceduralDungeons

First off, **thank you** for considering contributing! I truly believe in open source and the power of community collaboration. Unlike many repositories, I actively welcome contributions of all kinds - from bug fixes to new features.

## My Promise to Contributors

- **I will respond to every PR and issue** - I guarantee feedback on all contributions
- **Bug fixes are obvious accepts** - If it fixes a bug, it's getting merged
- **New features are welcome** - I'm genuinely open to new ideas and enhancements
- **Direct line of communication** - If I'm not responding to a PR or issue, email me directly at johnvondrashek@gmail.com

## How to Contribute

### Reporting Bugs

1. Check existing issues to avoid duplicates
2. Include your .NET version and relevant configuration
3. Provide a minimal reproduction case if possible
4. Include the seed and `FloorConfig` settings that triggered the issue

### Submitting Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run the tests: `dotnet test`
5. Commit your changes
6. Push to your fork
7. Open a Pull Request

### Development Setup

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/shepherd-procedural-dungeons.git

# Build the project
dotnet build

# Run tests
dotnet test

# Run a specific example
dotnet run --project examples/BasicExample
```

### What Makes a Good PR

- **Tests** - Add tests for new features or bug fixes when applicable
- **Documentation** - Update the README or wiki if adding new public APIs
- **Focused changes** - Keep PRs focused on a single concern
- **Clear description** - Explain what your PR does and why

### Areas Where Contributions Are Especially Welcome

- New constraint types for room placement
- Additional room template shapes
- Performance optimizations
- Examples and documentation
- Bug reports with reproduction cases

## Code of Conduct

This project follows the [Rule of St. Benedict](CODE_OF_CONDUCT.md) as its code of conduct.

## Questions?

- Open an issue
- Email: johnvondrashek@gmail.com
