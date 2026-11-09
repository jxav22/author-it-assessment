export function parseTags(raw: string): string[] {
  return raw
    .split(',')
    .map((tag) => tag.trim())
    .filter(Boolean)
}

export function formatTags(tags: string[]): string {
  return tags.join(', ')
}

export function tagsEqual(a: string[], b: string[]): boolean {
  if (a.length !== b.length) {
    return false
  }
  return a.every((tag, index) => tag === b[index])
}
