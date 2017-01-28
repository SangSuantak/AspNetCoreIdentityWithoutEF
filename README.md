# ASP.NET Core Identity without EF (Raw ADO.NET)
This is my attempt to implement ASP.NET Core Identity with raw ADO.NET. I hate that MS shoves EF everywhere, and the lack of documentations/examples make it worse.

This is a WIP but will be updated regularly.

Please suggest any improvements since i'm new to DI. I hope to learn a lot from this exercise.

### Known Issues/Problems
1. Started it as a POC so project structure will be changed.
2. Role store not implemented yet.

### Objectives
1. Do away with EF dependency in ASP.NET Core Identity
2. Include and implement Web API with token based authentication/authorization
3. Separate the Web API project as a standalone API
4. Create UI layer using a popular framework (Angular?)
5. Consume Web API from UI layer
