from pathlib import Path
import uuid
root=Path(__file__).resolve().parents[1]
p=root/'Assets/Scriptable Object/Config/ElfShopConfig.asset'
t=p.read_text(encoding='utf-8-sig');t=t[:t.index('  _redElf')]
for name,ids,kind in [('red',range(1,11),0),('blue',range(5,23),1),('yellow',range(18,29),0)]:
    t+='  _'+name+'ElfItems:\n'
    for id in ids:t+=f'  - ItemId: {id}\n    ItemType: {kind}\n'
p.write_text(t,encoding='utf-8')
(root/'Assets/Resources/Data/ElfShopConfig.asset').write_text(t,encoding='utf-8')
directory=root/'Assets/Resources/Data/Tools';directory.mkdir(exist_ok=True)
for p in (root/'Assets/Scriptable Object/CookWareUpgradeData').glob('*.asset'):
    text=p.read_text(encoding='utf-8-sig')
    for capacity,cost,restaurant,uses in [(6,4000,4,40),(8,8000,7,80)]:
        text+=f'  - _maxIngredientCount: {capacity}\n    _upgradeConditions:\n    - _type: 0\n      _amount: {cost}\n    - _type: 1\n      _amount: {restaurant}\n    - _type: 2\n      _amount: {uses}\n'
    (directory/p.name).write_text(text,encoding='utf-8')
for p in [root/'Assets/Scripts/Progression',root/'Assets/Resources/Data/Tools',*list((root/'Assets/Scripts/Progression').rglob('*')),*list((root/'Assets/Resources/Data').rglob('*'))]:
    if p.suffix=='.meta':continue
    meta=Path(str(p)+'.meta')
    if meta.exists():continue
    text='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
    if p.is_dir():text+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    elif p.suffix=='.cs':text+='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    else:text+='NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    meta.write_text(text,encoding='utf-8')
