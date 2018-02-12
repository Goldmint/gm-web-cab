import { Limits} from "./limits";
export interface User {
  id          ?: string;
  name        : string;
  email       ?: string;
  tfaEnabled  ?: boolean;
  verifiedL0  ?: boolean;
  verifiedL1  ?: boolean;
  challenges ?: string[];
  limits     ?: Limits;
  social     ?: {
    facebook  : string|null,
    github    : string|null,
    vkontakte : string|null,
    google    : string|null
  };
}
