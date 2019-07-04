export const environment = {
  production: false,
  isProduction: false,
  detectExtraRights: false,
  MMNetwork: {
    name: 'Rinkeby',
    index: 4
  },
  walletNetwork: 'test',
  sumusNetworkUrl: {
    mainnet: 'https://service.goldmint.io/sumus/mainnet/v1',
    testnet: 'https://service.goldmint.io/sumus/testnet/v1'
  },
  apiUrl: 'https://staging.goldmint.io/api/v1',
  marketApiUrl: 'https://staging.goldmint.io/market/v1',
  gasPriceLink: 'https://www.etherchain.org/api/gasPriceOracle',
  recaptchaSiteKey: '6LcuSTcUAAAAAGGcHJdRqDN1fEmtKjYue_872F0k',
  etherscanUrl: 'https://rinkeby.etherscan.io/tx/',
  etherscanContractUrl: "https://rinkeby.etherscan.io/address/",
  infuraUrl: 'https://service.goldmint.io/proxy/infura/rinkeby',
  etherscanGetABIUrl: 'https://api-rinkeby.etherscan.io',
  EthContractAddress: '0x30c695f0db4e63e287e5c2a567bb7ad4bca6da94',
  EthGoldContractAddress: '0xd67a3c707f901c510724703f150b1f2d94dc5ee6',
  EthMntpContractAddress: '0x160350f317b573f477473dd74c3bdfcf5e619da0',
  EthPoolContractAddress: '0x11c6a3f8974ab6b6a3720d9d86b21e260b5b173b',
  EthOldPoolContractAddress: '0xcD4aE63c113a29757C2afBe240B740324A339DC2',
  getLiteWalletLink: {
    chrome: 'https://chrome.google.com/webstore/detail/goldmint-lite-wallet/fnabdmcgpkkjjegokfcnfbpneacddpfh',
    firefox: 'https://addons.mozilla.org/ru/firefox/addon/goldmint-lite-wallet/'
  }
};
