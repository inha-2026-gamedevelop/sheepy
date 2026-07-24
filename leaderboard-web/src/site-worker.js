export default {
  async fetch(request, env) {
    const url = new URL(request.url)
    const isStaticAsset = url.pathname.includes('.')
    const assetRequest = isStaticAsset
      ? request
      : new Request(new URL('/index.html', url), request)

    return env.ASSETS.fetch(assetRequest)
  },
}
