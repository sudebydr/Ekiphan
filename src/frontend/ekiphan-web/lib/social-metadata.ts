import { getSiteOrigin } from "./site-url";

const origin = getSiteOrigin();

export const defaultSocialImage = origin
  ? new URL("/images/ekiphan-kitchen-hero.png", origin).toString()
  : undefined;
