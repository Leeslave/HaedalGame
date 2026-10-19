"""Check Unity serialized planning data without requiring an Editor license."""
from pathlib import Path
import re, sys, json, openpyxl
root=Path(__file__).resolve().parents[1]
book=openpyxl.load_workbook(sys.argv[1],data_only=True)
text=(root/'Assets/Resources/Data/RecipeDatabaseSO.asset').read_text(encoding='utf-8')
ingredient_text, recipe_text=text.split('  _recipes:\n')
def split_records(text,field):
    return {int(m.group(1)):m.group(0) for m in re.finditer(r'^  - '+field+r': (\d+)\n.*?(?=^  - '+field+r':|\Z)',text,re.M|re.S)}
ings=split_records(ingredient_text,'_ingredientId');recipes=split_records(recipe_text,'_recipeId')
assert len(ings)==28 and len(recipes)==22
def field(block,name):return re.search(r'^\s+'+name+r': (.+)$',block,re.M)[1]
def sheet(name):
    rows=list(book[name].values)
    return [dict(zip(rows[0],r)) for r in rows[1:] if r[0] is not None]
codes={}
for r in sheet('ingredient'):
    block=ings[r['ing_n']];codes[r['rec_n']]=r['ing_n']
    assert json.loads(field(block,'_ingredientName'))==r['ing_name']
    assert int(field(block,'_recipeCode'))==r['rec_n']
    assert int(field(block,'_isBasicSeasoning'))==int(r['ing_type']==302)
for r in sheet('menu'):
    block=recipes[r['menu_n']]
    assert json.loads(field(block,'_recipeName'))==r['m_name']
    assert float(field(block,'_price'))==r['price']
    assert float(field(block,'_cookTime'))==r['menu_t']
    assert int(field(block,'_classId'))==r['class_n']
    actual=[(int(a),int(b)) for a,b in re.findall(r'    - _ingredientId: (\d+)\n.*?      _amount: (\d+)',block,re.S)]
    expected=[(codes[int(c)],int(n)) for c,n in zip(str(r['recipe']).split(),str(r['recipe_cnt']).split())]
    assert actual==expected,(r['menu_n'],actual,expected)
assert sum(int(field(b,'_isBasicSeasoning')) for b in ings.values())==6
known={}
for meta in (root/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8',errors='replace'),re.M)
    if match:known.setdefault(match[1],[]).append(meta)
new_assets=list((root/'Assets/Resources/Data').rglob('*.asset'))
for asset in new_assets:
    assert Path(str(asset)+'.meta').exists(),asset
    for guid in re.findall(r'guid: (\w+)',asset.read_text(encoding='utf-8-sig')):assert guid in known,(asset,guid)
for source in (root/'Assets/Scripts/Progression').rglob('*.cs'):
    meta=Path(str(source)+'.meta');assert meta.exists(),source
    guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1];assert len(known[guid])==1,source
for path in (root/'Assets/Resources/Data/Tools').glob('*.asset'):
    capacities=[int(x) for x in re.findall(r'_maxIngredientCount: (\d+)',path.read_text(encoding='utf-8'))]
    assert capacities==sorted(capacities) and max(capacities)>=8,(path,capacities)
print('PASS: 22 recipes / 28 ingredients / 6 unlimited seasonings match the supplied workbook.')
print('PASS: recipe quantities, prices, cooking types and times; new asset references and metadata; tool progression.')
