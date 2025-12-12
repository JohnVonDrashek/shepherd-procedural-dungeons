# Feature: Test Timeout Infrastructure

**ID**: FEATURE-001
**Status**: complete
**Created**: 2025-01-27T00:00:00Z
**Priority**: high
**Complexity**: medium

## Description

Implement a comprehensive test timeout infrastructure to prevent infinite execution times for problematic tests, improve CI/CD reliability, and provide better visibility into test performance. This feature addresses a critical gap in test infrastructure where tests can hang indefinitely, blocking CI pipelines and wasting developer time.

**Why this matters**: Without timeout protection, a single buggy test can cause entire test suites to hang, blocking CI/CD pipelines and making it impossible to detect when tests are taking too long. This feature enables proactive detection of performance regressions, prevents CI timeouts from external limits, and provides developers with actionable performance data.

**Problems solved**:
- Tests hanging indefinitely due to infinite loops or deadlocks
- CI/CD pipelines timing out at the job level instead of individual tests
- No visibility into which tests are slow or approaching timeout thresholds
- Inability to set appropriate timeout values based on test complexity
- Difficulty identifying performance regressions in test execution

## Requirements

- [x] Per-test timeout configuration via xUnit attributes (`[Fact(Timeout = milliseconds)]`)
- [x] Global default timeout configuration via xUnit configuration file or test project settings
- [x] Timeout reporting that identifies tests approaching or exceeding timeouts
- [x] Timeout best practices documentation in wiki
- [x] Integration with existing test infrastructure (xUnit, TestHelpers)
- [x] Support for different timeout values based on test categories (unit, integration, performance)
- [x] Clear timeout error messages that help identify the cause of timeouts

## Technical Details

**Implementation Approach**:

1. **xUnit Timeout Attributes**: Leverage xUnit's built-in timeout support via `[Fact(Timeout = milliseconds)]` and `[Theory(Timeout = milliseconds)]` attributes. Apply appropriate timeouts to existing tests based on their complexity:
   - Unit tests: 1-5 seconds (fast, isolated)
   - Integration tests: 10-30 seconds (full generation cycles)
   - Performance/stress tests: 60+ seconds (large dungeon generation)

2. **Global Configuration**: Create `xunit.runner.json` configuration file to set default timeouts for all tests, with ability to override per-test. This provides a safety net even if individual tests don't specify timeouts.

3. **Test Categorization**: Use xUnit traits or custom attributes to categorize tests (e.g., `[Trait("Category", "Integration")]`) and apply category-specific default timeouts.

4. **Timeout Reporting**: Enhance test output to include:
   - Tests that exceeded their timeout
   - Tests that took >80% of their timeout (warning threshold)
   - Summary statistics of test execution times
   - Integration with existing test reporting infrastructure

5. **TestHelpers Enhancement**: Add helper methods or attributes to `TestHelpers.cs` for common timeout patterns (e.g., `[IntegrationTest]` attribute that sets appropriate timeout).

6. **Documentation**: Create wiki documentation covering:
   - How to set timeouts on tests
   - Best practices for timeout values
   - How to diagnose timeout failures
   - When to increase vs. fix slow tests

**Architecture Considerations**:

- This feature touches the test infrastructure layer, requiring changes to test project configuration and potentially test helper utilities
- Must integrate seamlessly with existing xUnit test framework
- Should not break existing tests that don't specify timeouts (graceful degradation)
- Timeout values should be configurable without code changes (via configuration files)
- Consider CI/CD environment variables for timeout overrides in different environments

**API Design**:

- Use standard xUnit attributes: `[Fact(Timeout = 5000)]` for per-test timeouts
- Configuration file: `xunit.runner.json` with `maxParallelThreads` and timeout settings
- Custom attributes (optional): `[IntegrationTest]`, `[PerformanceTest]` with sensible defaults
- TestHelpers extensions: Helper methods for timeout-aware test execution

## Dependencies

- None

## Test Scenarios

1. **Per-Test Timeout Enforcement**: A test with `[Fact(Timeout = 1000)]` should fail if it takes longer than 1 second, with a clear timeout error message.

2. **Global Default Timeout**: Tests without explicit timeouts should use a global default (e.g., 30 seconds) configured in `xunit.runner.json`.

3. **Timeout Reporting**: When tests timeout, the test output should clearly indicate which test timed out and what the timeout value was.

4. **Category-Based Timeouts**: Tests marked with `[Trait("Category", "Integration")]` should use integration test timeout defaults (e.g., 30 seconds) while unit tests use shorter defaults (e.g., 5 seconds).

5. **Slow Test Detection**: Test output should warn about tests that take >80% of their timeout threshold, helping identify tests that are approaching limits.

6. **CI/CD Integration**: Timeout configuration should work correctly in CI/CD environments, preventing job-level timeouts from masking individual test timeouts.

## Acceptance Criteria

- [x] All existing tests have appropriate timeout values set (either explicitly or via defaults)
- [x] `xunit.runner.json` configuration file exists with sensible default timeout values
- [x] Tests that exceed their timeout fail with clear error messages indicating timeout
- [x] Test output includes warnings for tests approaching timeout thresholds (>80% of timeout)
- [x] Wiki documentation exists explaining timeout configuration and best practices
- [x] TestHelpers includes helper methods/attributes for common timeout patterns
- [x] All tests pass with timeout infrastructure in place (no false positives)
- [x] Timeout values are documented and justified in code comments or wiki
