# Release checklist

1. Run the full .NET, client, decoder, documentation, and sample-host checks from the repository README.
2. Run the supported Umbraco 17/18 compatibility matrix and resolve all dependency-audit findings.
3. Inspect the NuGet package contents, update `CHANGELOG.md`, and set the package version in the project and Marketplace metadata.
4. Pack the package and publish it to the intended NuGet feed, then submit the matching Marketplace metadata.
