# Aevatar Framework Architecture

## Overview

Aevatar Framework is a distributed actor-based framework built on top of Microsoft Orleans, designed for creating scalable event-sourced applications. It provides a robust foundation for implementing complex distributed systems with built-in support for:

- Event sourcing
- Pub/sub messaging
- State management
- Hierarchical agent relationships

The framework follows a component-based architecture with clear separation of concerns and uses the Actor model as its fundamental abstraction.

## Core Architecture Components

### 1. Layered Architecture

The framework is organized into several logical layers:

```
┌─────────────────────────────────────────────────────────┐
│                       Applications                       │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────┐
│                       Aevatar Framework                  │
├─────────────────────────────────────────────────────────┤
│                        Plugins Layer                     │
├─────────────────────────────────────────────────────────┤
│                         Core Layer                       │
├─────────────────────────────────────────────────────────┤
│                      Abstractions Layer                  │
├─────────────────────────────────────────────────────────┤
│                   Event Sourcing Layer                   │
├─────────────────────────────────────────────────────────┤
│                    Microsoft Orleans                     │
└─────────────────────────────────────────────────────────┘
```

#### Abstractions Layer (Aevatar.Core.Abstractions)
- Defines the fundamental interfaces and abstract classes
- Provides contracts for the Actor model implementation
- Contains base state and event classes

#### Core Layer (Aevatar.Core)
- Implements the GAgent paradigm - the foundation of the framework
- Provides event sourcing, state management, and pub/sub capabilities
- Handles agent registration and hierarchical relationships

#### Event Sourcing Layer (Aevatar.EventSourcing.Core, Aevatar.EventSourcing.MongoDB)
- Implements event storage and retrieval mechanisms
- Provides consistent event logging and replay capabilities
- Supports multiple storage backends (MongoDB implementation provided)

#### Plugins Layer (Aevatar.Plugins)
- Offers extension capabilities for adding specialized functionality
- Provides a mechanism for registering and managing plugins

### 2. Core Components

#### GAgentBase

The `GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>` class is the cornerstone of the framework. It extends Orleans' `JournaledGrain` and provides:

```
┌───────────────────────────────────────────────┐
│             GAgentBase                         │
├───────────────────────────────────────────────┤
│ - State Management                             │
│ - Event Publishing/Subscription                │
│ - Hierarchical Agent Structure                 │
│ - Event Handling                               │
└─────────────────┬─────────────────────────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
┌───────┴────────┐  ┌───────┴────────┐
│ BroadCastAgent │  │StateProjection │
│                │  │    Agent       │
└────────────────┘  └────────────────┘
```

Main responsibilities:
- **Event Sourcing**: Uses Orleans' `JournaledGrain` for reliable event storage and replay
- **State Management**: Manages agent state with automatic persistence
- **Event Publishing**: Supports publishing and subscribing to events between agents
- **Hierarchical Structure**: Allows agents to register with and subscribe to other agents

#### Agent Types

The framework provides several specialized agent types:

1. **GAgentBase**: The foundation agent with basic event sourcing capabilities
2. **StateProjectionGAgentBase**: Extends GAgentBase with projection capabilities
3. **BroadCastGAgentBase**: Specializes in broadcasting events to registered children
4. **ArtifactGAgent**: Connects GAgents with domain artifacts

#### Key Interfaces

- **IGAgent**: Base interface for all agents
- **IStateGAgent**: Extends IGAgent with state management capabilities
- **IExtGAgent**: Extension interface for additional functionality
- **IArtifactGAgent**: Interface for connecting domain artifacts with agents

### 3. State Management

The framework uses strongly-typed state containers that extend from `StateBase`. Every state change is tracked through events, following the event sourcing pattern.

```
┌───────────────┐      ┌─────────────────┐
│   StateBase   │ ◄─── │ StateLogEventBase│
└───────┬───────┘      └─────────┬───────┘
        │                        │
        │                        │
┌───────┴───────┐      ┌─────────┴───────┐
│ ConcreteState │ ◄─── │ConcreteStateLog │
└───────────────┘      │     Event       │
                       └─────────────────┘
```

### 4. Event Processing

The framework implements a sophisticated event processing mechanism:

```
┌────────────┐    ┌────────────┐    ┌─────────────┐
│ RaiseEvent │───►│EventHandler│───►│State Update │
└────────────┘    └────────────┘    └─────────────┘
       │                                   │
       │                                   │
       ▼                                   ▼
┌────────────┐                      ┌─────────────┐
│ Journal    │                      │ Persistence │
└────────────┘                      └─────────────┘
       │
       │
       ▼
┌────────────┐    ┌────────────┐
│Stream      │───►│Subscribers │
│Publication │    │            │
└────────────┘    └────────────┘
```

1. Events are raised through `RaiseEvent` method
2. Events are processed by appropriate event handlers
3. State is updated based on event processing
4. Events are journaled for event sourcing
5. Events are published to streams for subscribers
6. State changes are persisted

### 5. Hierarchical Agent Structure

The framework supports a hierarchical relationship between agents:

```
       ┌───────────┐
       │  Parent   │
       │   Agent   │
       └─────┬─────┘
             │
    ┌────────┴────────┐
    │                 │
┌───┴───┐        ┌────┴───┐
│ Child │        │ Child  │
│ Agent │        │ Agent  │
└───┬───┘        └────┬───┘
    │                 │
┌───┴───┐        ┌────┴───┐
│Grandchild      │Grandchild
│ Agent │        │ Agent  │
└───────┘        └────────┘
```

- Agents can register child agents
- Agents can subscribe to parent agents
- Events can flow up and down the hierarchy

### 6. Stream Processing

Aevatar Framework integrates deeply with Orleans' streaming capabilities:

```
┌────────────┐    ┌───────────────┐    ┌────────────┐
│Event Source│───►│Stream Provider│───►│ Subscribers│
└────────────┘    └───────────────┘    └────────────┘
```

- Built-in stream provider integration
- Automatic stream subscription management
- Support for event forwarding

## Implementation Details

### Event Handling

Event handlers are implemented using the `[EventHandler]` attribute:

```csharp
[EventHandler]
public async Task HandleCustomEventAsync(CustomEvent event)
{
    // Handle the event
}
```

The framework automatically routes events to the appropriate handlers.

### Agent Registration

Agents can form hierarchical relationships through registration:

```csharp
await parent.RegisterAsync(child);
```

This sets up the parent-child relationship and establishes the event flow between agents.

## Deployment Considerations

The Aevatar Framework is designed to run in a distributed environment, with:

- Multiple silos for high availability
- Clustered deployment for scalability
- Support for different storage backends

## Conclusion

The Aevatar Framework provides a comprehensive foundation for building scalable, event-sourced applications using the Actor model. Its layered architecture, combined with the power of Orleans, enables developers to create complex distributed systems while maintaining clean separation of concerns and robust event handling.
