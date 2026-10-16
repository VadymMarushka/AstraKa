using Microsoft.AspNetCore.Authorization;

namespace AstraKa.Api.Auth;

/// <summary>
/// "Author or Oracle may edit" - the resource-based authorization example from CLAUDE.md, used by
/// future Course/Exercise edit/delete endpoints. See <see cref="AuthorOrOracleAuthorizationHandler"/>.
/// </summary>
internal sealed class AuthorOrOracleRequirement : IAuthorizationRequirement;
