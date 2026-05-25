# Tasks — RF-EXP-04

Objective: Implement an isolated vertical slice that constructs and delivers an `EnrichedConversationMessage` over WebSocket using Clean Architecture (.NET 10).

## Overview
This tasks file breaks the work into API, Application, and Infrastructure steps. Keep all changes isolated to the `002-rf-exp-04-enriched-response` feature slice.

## Tasks

1. Add domain enums
- Goal: Declare `EmotionType`, `MessageSource`, and `MessageType` enums and any small value objects in `turning.Domain`.
- Deliverables: enum types, unit tests validating enum values.
- Files: `turning.Domain/Entities/` or `turning.Domain/Common/`.

2. Add DTO contract
- Goal: Add `EnrichedConversationMessageDto` to `turning.Application` ensuring `System.Text.Json` property names match spec.
- Deliverables: DTO class with JSON attributes, serialization unit test.
- Files: `turning.Application/DTOs/EnrichedConversationMessageDto.cs`.

3. Create response factory interface
- Goal: Define `IEnrichedResponseFactory` in `turning.Application` to encapsulate payload construction.
- Deliverables: interface definition with method to build DTO from domain inputs.
- Files: `turning.Application/Features/ConversationTurns/EnrichedResponse/IEnrichedResponseFactory.cs`.

4. Implement response factory
- Goal: Implement `EnrichedResponseFactory` to create deterministic demo payloads; map domain model -> DTO.
- Deliverables: factory implementation + unit tests for produced payload fields and defaults.
- Files: `turning.Application/Features/ConversationTurns/EnrichedResponse/EnrichedResponseFactory.cs`.

5. Define websocket abstraction
- Goal: Add `IWebSocketTransport` abstraction in `turning.Application` to represent sending JSON messages to clients.
- Deliverables: interface with `Task SendAsync(string sessionId, object payload, CancellationToken)` signature.
- Files: `turning.Application/Interfaces/IWebSocketTransport.cs`.

6. Implement websocket transport
- Goal: Create a concrete `WebSocketTransport` in `turning.Infrastructure` that uses ASP.NET Core WebSocket primitives and `System.Text.Json` for serialization.
- Deliverables: transport implementation, connection manager (if needed), and unit tests for serialization logic.
- Files: `turning.Infrastructure/WebSockets/WebSocketTransport.cs` and optional `WebSocketConnectionManager.cs`.

7. Add API websocket endpoint
- Goal: Add an endpoint `/ws/enriched-response/{sessionId}` in `turning.API` that upgrades to WebSocket, accepts connections, and uses `IEnrichedResponseFactory` + `IWebSocketTransport` to send payloads.
- Deliverables: WebSocket endpoint handler, minimal request routing, integration test to open connection.
- Files: `turning.API/WebSockets/EnrichedResponseWebSocketHandler.cs`, `turning.API/Program.cs` (routing), and potentially a small controller.

8. Register DI services
- Goal: Register `IEnrichedResponseFactory`, `IWebSocketTransport`, and any required supporting services in DI with scoped/singleton lifetimes as appropriate.
- Deliverables: DI registration changes confined to the feature slice.
- Files: `turning.API/DependencyInjection/ServiceExtensions.cs` or `turning.Infrastructure/DependencyInjection`.

9. Unit tests
- Goal: Add unit tests for DTO serialization, factory logic, and websocket abstraction interfaces.
- Deliverables: tests under `tests/turning.Application.Tests` or `tests/turning.Domain.Tests`.
- Files: `tests/turning.Application.Tests/*`.

10. Integration tests
- Goal: Add an integration test that starts the API, connects to `/ws/enriched-response/{sessionId}`, and verifies a valid enriched JSON payload is received.
- Deliverables: integration test project or test case using `WebSocketClient`.
- Files: `tests/turning.Infrastructure.Tests/` or `tests/turning.API.IntegrationTests/`.

11. Documentation
- Goal: Add `README.md` notes for how to run the websocket endpoint locally and expected payload schema.
- Deliverables: docs in `specs/002-rf-exp-04-enriched-response/README.md` and short usage examples.

12. Isolation review
- Goal: Verify no unrelated modules or controllers were modified; perform quick code review and tidy up.
- Deliverables: review checklist and commit message guidance.

## Order & Estimates
- Low-risk ordering: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12
- Rough estimates (dev-only): 2-4h per task on average; tests + integration 6-10h.

## Acceptance
- All unit and integration tests pass for the new slice.
- Endpoint returns the JSON payload exactly matching `spec.md`.
- Feature changes are limited to files listed above.

---

If you want, I can now scaffold the DTOs, interfaces, and a minimal WebSocket endpoint in the codebase and run the tests locally.