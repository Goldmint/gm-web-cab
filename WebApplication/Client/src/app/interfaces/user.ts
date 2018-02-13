export interface User {
  id          ?: string;
  name        ?: string;
  email       ?: string;
  tfaEnabled  ?: boolean;
  verifiedL0  ?: boolean;
  verifiedL1  ?: boolean;
  challenges  ?: string[];
}
