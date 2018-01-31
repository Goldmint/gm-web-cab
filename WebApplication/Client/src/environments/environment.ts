// The file contents for the current environment will overwrite these during build.
// The build system defaults to the dev environment which uses `environment.ts`, but if you do
// `ng build --env=prod` then `environment.prod.ts` will be used instead.
// The list of which env maps to which file can be found in `.angular-cli.json`.

export const environment = {
  production: false,
  // apiUrl: 'http://localhost:8000/api/v1',
  apiUrl: 'https://app.goldmint.io/api/v1',
  // apiUrl: 'http://gm-cabinet-dev.pashog.net/api-sandbox.php?action=/api',
  // recaptchaSiteKey: '6LfqWzwUAAAAAKd9gu2-jDLvIKYPMm_aneMp1enn'
  recaptchaSiteKey: '6LcuSTcUAAAAAGGcHJdRqDN1fEmtKjYue_872F0k'
};
