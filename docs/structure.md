# Aevatar Framework Project Structure

This document outlines the code structure of the Aevatar Framework, providing a map of the codebase organization.

## Overview

The Aevatar Framework codebase is organized into the following main directories:

```
aevatar-framework/
├── docs/                    # Documentation files
├── samples/                 # Example applications demonstrating framework usage
├── src/                     # Source code for the framework components
├── test/                    # Test projects
├── .github/                 # GitHub workflows and configuration
├── aevatar-framework.sln    # Visual Studio solution file
├── common.props             # Common MSBuild properties
├── Directory.Build.props    # Global build properties
├── Directory.Packages.props # Central package version management
├── LICENSE                  # License information
└── README.md                # Project overview and documentation
```

## Source Code Structure (`src/`)

The source code is organized into multiple projects, each with a specific responsibility:

```
src/
├── Aevatar.Core/                     # Core implementation of the GAgent framework
├── Aevatar.Core.Abstractions/        # Interfaces and abstract classes
├── Aevatar/                          # Main framework module and integration 
├── Aevatar.EventSourcing.Core/       # Event sourcing core implementation
├── Aevatar.EventSourcing.MongoDB/    # MongoDB adapter for event sourcing
├── Aevatar.Plugins/                  # Plugin system implementation
└── Aevatar.PermissionManagement/     # Permission management system
```

### Aevatar.Core

The core implementation contains the fundamental GAgent classes and supporting components:

```
Aevatar.Core/
├── Extensions/                      # Extension methods
├── Observability/                   # Monitoring and logging
├── Projections/                     # State projection components
├── GAgentBase.cs                    # Main GAgent base class
├── GAgentBase.Observers.cs          # Observer pattern implementation
├── GAgentBase.Publish.cs            # Event publishing functionality
├── GAgentBase.Subscribe.cs          # Subscription management
├── GAgentBase.SyncWorker.cs         # Synchronization support
├── BroadCastGAgentBase.cs           # Broadcasting agent implementation
├── StateProjectionGAgentBase.cs     # Projection agent implementation
├── ArtifactGAgent.cs                # Artifact agent implementation
├── GAgentFactory.cs                 # Factory for creating GAgents
├── GAgentManager.cs                 # Manager for GAgent instances
├── StateDispatcher.cs               # Dispatcher for state changes
└── Aevatar.Core.csproj              # Project file
```

### Aevatar.Core.Abstractions

The abstractions layer defines the contracts and base types:

```
Aevatar.Core.Abstractions/
├── Application/                    # Application-level abstractions
├── Events/                         # Event-related interfaces and base classes
├── Exceptions/                     # Exception definitions
├── Extensions/                     # Extension methods
├── Infrastructure/                 # Infrastructure components
├── Plugin/                         # Plugin system interfaces
├── Projections/                    # Projection abstractions
├── SyncWorker/                     # Synchronization interfaces
├── IGAgent.cs                      # Base agent interface
├── IStateAgent.cs                  # State agent interface
├── IExtGAgent.cs                   # Extended agent interface
├── IArtifact.cs                    # Artifact interface
├── IArtifactGAgent.cs              # Artifact agent interface
├── StateBase.cs                    # Base class for all state objects
├── StateLogEventBase.cs            # Base class for state log events
├── ConfigurationBase.cs            # Base class for configuration
└── Aevatar.Core.Abstractions.csproj # Project file
```

### Aevatar.EventSourcing.Core

The event sourcing core implementation:

```
Aevatar.EventSourcing.Core/
├── Exceptions/                       # Event sourcing specific exceptions
├── Hosting/                          # Hosting components
├── LogConsistency/                   # Log consistency components
├── Snapshot/                         # Snapshot management
├── Storage/                          # Storage abstractions and base implementations
├── InMemoryLogConsistentStorage.cs   # In-memory implementation for testing
└── Aevatar.EventSourcing.Core.csproj # Project file
```

### Aevatar.EventSourcing.MongoDB

MongoDB implementation for event sourcing:

```
Aevatar.EventSourcing.MongoDB/
├── MongoDBLogConsistentStorage.cs    # MongoDB storage implementation
└── Aevatar.EventSourcing.MongoDB.csproj # Project file
```

### Aevatar.Plugins

Plugin system implementation:

```
Aevatar.Plugins/
├── PluginGAgentManager.cs           # Manager for plugin agents
└── Aevatar.Plugins.csproj           # Project file
```

## Sample Applications (`samples/`)

The framework includes several sample applications demonstrating different usage patterns:

```
samples/
├── ArtifactGAgent/                 # Demonstrates using Artifact agents
│   ├── ArtifactGAgent.Client/
│   └── ArtifactGAgent.Silo/
├── BroadCastGAgentDemo/            # Demonstrates broadcasting functionality
├── MessagingGAgent.Client/         # Messaging agent client demo
├── MessagingGAgent.Grains/         # Messaging agent grain implementations
├── MessagingGAgent.Silo/           # Messaging agent server 
├── PluginGAgent/                   # Demonstrates plugin system
│   ├── PluginGAgent.Client/
│   ├── PluginGAgent.Grains/
│   └── PluginGAgent.Silo/
├── PubSubDemoWithoutGroup/         # Pub/sub pattern demonstration
└── SimpleAIGAgent/                 # AI agent example
```

## Test Structure (`test/`)

The testing projects are organized to verify different aspects of the framework:

```
test/
├── Aevatar.AI.AIGAgent.Tests/      # AI agent tests
├── Aevatar.ArtifactGAgents/        # Artifact agent tests
├── Aevatar.Core.Tests/             # Core functionality tests
├── Aevatar.EventSourcing.MongoDB.Tests/ # MongoDB event sourcing tests
├── Aevatar.GAgents.Plugins/        # Plugin tests
├── Aevatar.GAgents.Tests/          # GAgent implementation tests
├── Aevatar.TestBase/               # Common test utilities and fixtures
├── OrleansTestKit/                 # Orleans testing toolkit
└── OrleansTestKit.Tests/           # Tests for the Orleans testing toolkit
```

## Build System

The build system is based on MSBuild with central package version management:

- `Directory.Build.props`: Global build properties
- `Directory.Packages.props`: Centralized package version management
- `common.props`: Common properties shared across projects

The solution uses modern .NET SDK-style projects with a clean separation of concerns.

## Documentation (`docs/`)

The documentation includes:

```
docs/
├── architecture.md        # High-level architecture description
└── structure.md           # This code structure document
```

## Development Workflow

The project follows a standard .NET development workflow:

1. Solution is built using standard .NET CLI commands (`dotnet build`)
2. Tests can be run using `dotnet test`
3. The framework is designed to be consumed as NuGet packages
