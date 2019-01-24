export const environment = {
  production: false,
  isProduction: false,
  detectExtraRights: false,
  MMNetwork: {
    name: 'Rinkeby',
    index: 4
  },
  // sumusNetworkUrl: {
  //   MainNet: 'https://service.goldmint.io/sumus/mainnet/v1',
  //   TestNet: 'https://service.goldmint.io/sumus/testnet/v1'
  // },
  sumusNetworkUrl: 'https://service.goldmint.io/sumus/testnet/v1',
  apiUrl: 'http://localhost:8000/api/v1',
  walletApiUrl: 'https://staging.goldmint.io/wallet/api/v1',
  marketApiUrl: 'https://staging.goldmint.io/market/v1',
  gasPriceLink: 'https://www.etherchain.org/api/gasPriceOracle',
  recaptchaSiteKey: '6LcuSTcUAAAAAGGcHJdRqDN1fEmtKjYue_872F0k',
  etherscanUrl: 'https://rinkeby.etherscan.io/tx/',
  infuraUrl: 'https://service.goldmint.io/proxy/infura/rinkeby',
  etherscanGetABIUrl: 'https://api-rinkeby.etherscan.io',
  EthContractAddress: '0x30c695f0db4e63e287e5c2a567bb7ad4bca6da94',
  EthGoldContractAddress: '0xd67a3c707f901c510724703f150b1f2d94dc5ee6',
  EthMntpContractAddress: '0x160350f317b573f477473dd74c3bdfcf5e619da0',
  EthPoolContractAddress: '0xcD4aE63c113a29757C2afBe240B740324A339DC2',
  getLiteWalletLink: 'https://chrome.google.com/webstore/detail/goldmint-lite-wallet/fnabdmcgpkkjjegokfcnfbpneacddpfh'
};
