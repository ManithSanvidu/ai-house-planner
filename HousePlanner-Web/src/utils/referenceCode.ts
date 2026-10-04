type ReferenceId = string | null | undefined;

const formatReference = (prefix: string, id: ReferenceId): string => {
 const compactId = id?.replaceAll('-', '').trim();
 if (!compactId) return `${prefix}-UNKNOWN`;
 return `${prefix}-${compactId.slice(0, 8).toUpperCase()}`;
};

export const formatDesignRef = (id: ReferenceId): string => formatReference('DES', id);
export const formatProjectRef = (id: ReferenceId): string => formatReference('PRJ', id);
export const formatRequestRef = (id: ReferenceId): string => formatReference('REQ', id);
