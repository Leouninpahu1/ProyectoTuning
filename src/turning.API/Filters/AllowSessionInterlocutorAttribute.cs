namespace Turning.API.Filters;

/// <summary>
/// Permite que el interlocutor humano asignado a la sesión use esta ruta.
/// </summary>
/// <remarks>
/// Es opt-in, no por omisión, y esa es la decisión de diseño importante: sin este atributo
/// toda ruta con <c>{sessionId}</c> sigue exigiendo ser el dueño. Marcarlo obliga a pensar,
/// endpoint por endpoint, si el interlocutor tiene algo que hacer ahí.
///
/// Concretamente, lo llevan las rutas de conversación y la consulta de la sesión. <b>No</b>
/// lo llevan resultados, encuesta, emociones ni el ciclo de vida: el interlocutor participa
/// en la conversación, no es dueño del experimento del participante.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AllowSessionInterlocutorAttribute : Attribute
{
}
