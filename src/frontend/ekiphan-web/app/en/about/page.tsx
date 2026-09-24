import { AboutExperience, aboutMetadata } from "../../../components/about-experience";

export const metadata = aboutMetadata("en");

export default function EnglishAboutPage() {
  return <AboutExperience locale="en" />;
}
