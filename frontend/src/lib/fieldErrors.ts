/** Resolve Problem Details field errors for a camelCase form field. */
export function fieldErrors(
  errors: Record<string, string[]> | undefined,
  field: string,
): string[] {
  if (!errors) {
    return []
  }

  const exact = errors[field]
  if (exact?.length) {
    return exact
  }

  const needle = field.toLowerCase()
  return Object.entries(errors)
    .filter(([key]) => {
      const normalized = key.toLowerCase()
      return (
        normalized === needle ||
        normalized.startsWith(`${needle}[`) ||
        normalized.startsWith(`${needle}.`) ||
        normalized.endsWith(`.${needle}`)
      )
    })
    .flatMap(([, messages]) => messages)
}
