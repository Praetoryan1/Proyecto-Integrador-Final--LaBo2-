export async function solicitar(
  url,
  { csrf, method = "GET", data, signal } = {},
) {
  const response = await fetch(url, {
    method,
    credentials: "same-origin",
    signal,
    headers: {
      Accept: "application/json",
      ...(data === undefined ? {} : { "Content-Type": "application/json" }),
      ...(csrf ? { "X-CSRF-TOKEN": csrf } : {}),
    },
    body: data === undefined ? undefined : JSON.stringify(data),
  });
  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("json") ? await response.json() : null;
  if (
    !response.ok ||
    response.redirected ||
    (response.status !== 204 && !body)
  ) {
    const validation = body?.errors ? Object.values(body.errors).flat() : [];
    let message = validation.length ? validation.join(" ") : body?.detail;
    if (response.status === 401 || response.redirected)
      message =
        "Tu sesión terminó. Copiá tu texto e ingresá de nuevo antes de continuar.";
    if (response.status === 403)
      message = "Tu cuenta no tiene permiso para esta operación.";
    if (!message)
      message =
        response.status === 400
          ? "No se pudo validar la solicitud. Revisá los campos; si tu sesión cambió, copiá el texto y recargá la página."
          : "No se pudo completar la solicitud. Intentá nuevamente.";
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }
  return body;
}
