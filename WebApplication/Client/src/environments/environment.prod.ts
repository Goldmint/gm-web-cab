export const environment = {
  production: true,
  isProduction: true,
  detectExtraRights: true,
  MMNetwork: {
    name: 'Main',
    index: 1
  },
  walletNetwork: 'main',
  sumusNetworkUrl: {
    mainnet: 'https://service.goldmint.io/sumus/mainnet/v1',
    testnet: 'https://service.goldmint.io/sumus/testnet/v1'
  },
  apiUrl: 'https://app.goldmint.io/api/v1',
  marketApiUrl: 'https://service.goldmint.io/pawnmarket/v1',
  gasPriceLink: 'https://www.etherchain.org/api/gasPriceOracle',
  recaptchaSiteKey: '6LcuSTcUAAAAAGGcHJdRqDN1fEmtKjYue_872F0k',
  etherscanUrl: 'https://etherscan.io/tx/',
  etherscanContractUrl: "https://etherscan.io/address/",
  infuraUrl: 'https://service.goldmint.io/proxy/infura/mainnet',
  etherscanGetABIUrl: 'https://api.etherscan.io',
  EthContractAddress: '0xa5dc5b5046003fa379ac6430675b543fcb69f101',
  EthMntpContractAddress: '0x83cee9e086A77e492eE0bB93C2B0437aD6fdECCc',
  EthPoolContractAddress: '0x9568C8C783f7166A9b88d0047ad28EFC43921242',
  EthOldPoolContractAddress: '0x02ad0e74f0e2e4ce093aa7517901ac32f0abd370',
  SwapContractAddress: '0xdfad4474999773137c131c1c8cb343ed150c95ec',
  getLiteWalletLink: {
    chrome: 'https://chrome.google.com/webstore/detail/goldmint-lite-wallet/fnabdmcgpkkjjegokfcnfbpneacddpfh',
    firefox: 'https://addons.mozilla.org/ru/firefox/addon/goldmint-lite-wallet/'
  }
};
