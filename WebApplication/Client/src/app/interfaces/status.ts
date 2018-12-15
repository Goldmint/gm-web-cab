export interface Status {
  data: {
    ethereum: {
      goldToken: string,
      migrationAddress: string,
      mintToken: string
    },
    sumus: {
      migrationAddress: string
    }
  }
}