export type GalleryTranslation = {
  languageCode: "tr" | "en";
  title: string;
  caption?: string | null;
};

export type AdminGalleryItem = {
  id: string;
  mediaAssetId: string;
  sortOrder: number;
  isPublished: boolean;
  translations: GalleryTranslation[];
};
