export type MenuTranslation = {
  languageCode: "tr" | "en";
  label: string;
};

export type AdminMenuItem = {
  id: string;
  code: string;
  location: number;
  url: string;
  isExternal: boolean;
  openInNewTab: boolean;
  parentId: string | null;
  sortOrder: number;
  isPublished: boolean;
  translations: MenuTranslation[];
};

export type PublicMenuItem = {
  id: string;
  parentId: string | null;
  label: string;
  url: string;
  isExternal: boolean;
  openInNewTab: boolean;
  sortOrder: number;
};

