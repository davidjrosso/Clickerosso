#!/usr/bin/env python3
"""Create stable metadata for new assets only. Never replace existing GUIDs."""
from pathlib import Path
import hashlib

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
for path in sorted(assets.rglob('*')):
    if path.name.endswith('.meta'):
        continue
    meta = Path(str(path) + '.meta')
    if meta.exists():
        continue
    relative = path.relative_to(root).as_posix()
    guid = hashlib.md5(('ReactorDeCaos:' + relative).encode()).hexdigest()
    text = 'fileFormatVersion: 2\nguid: ' + guid + '\n'
    if path.is_dir():
        text += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif path.suffix == '.cs':
        text += 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    elif path.suffix == '.png':
        text += '''TextureImporter:
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 50
  textureType: 0
  textureShape: 1
  alphaIsTransparency: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''
    elif path.suffix == '.wav':
        text += '''AudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.7
    conversionMode: 0
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 0
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''
    meta.write_text(text)
