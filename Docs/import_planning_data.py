"""Import the provided planning workbook into the existing Unity database asset.
Usage: python Docs/import_planning_data.py workbook.xlsx
Ingredient purchase prices are prototype balance values; the workbook has no price column.
"""
from pathlib import Path
import sys, json, re, openpyxl
root=Path(__file__).resolve().parents[1]
workbook=openpyxl.load_workbook(sys.argv[1],data_only=True)
def rows(name):
    values=list(workbook[name].values)
    return [dict(zip(values[0],row)) for row in values[1:] if row[0] is not None]
ings=rows('ingredient'); recipes=rows('menu')
asset=root/'Assets/Resources/Data/RecipeDatabaseSO.asset'
original=asset.read_text(encoding='utf-8-sig')
prefix=original[:original.index('  _ingredients:')]
def q(value): return json.dumps(str(value),ensure_ascii=False)
lines=[prefix.rstrip(),'  _ingredients:']
for i in ings:
    basic=i['ing_type']==302
    lines.extend([f"  - _ingredientId: {i['ing_n']}", f"    _ingredientName: {q(i['ing_name'])}",f"    _recipeCode: {i['rec_n']}",f"    _price: {0 if basic else 5 if i['ing_type']==303 else 8}",'    _belongIsland: 1','    _icon: {fileID: 0}','    _defaultUnlock: 1',f'    _isBasicSeasoning: {int(basic)}'])
lines.append('  _recipes:')
bycode={i['rec_n']:i for i in ings}
for r in recipes:
    stars=0 if str(r['grade'])=='기본' else int(re.search(r'\d+',str(r['grade']))[0]) if re.search(r'\d+',str(r['grade'])) else str(r['grade']).count('★')
    lines.extend([f"  - _recipeId: {r['menu_n']}",f"    _recipeName: {q(r['m_name'])}",f'    _recipeGrade: {stars}',f"    _unlockType: {r['type_n']}",f"    _classId: {r['class_n']}",f"    _cookTime: {r['menu_t']}",f"    _rawRecipeText: {q(r['recipe'])}",'    _requirements:'])
    codes=str(r['recipe']).split();counts=str(r['recipe_cnt']).split()
    assert len(codes)==len(counts)
    for code,count in zip(codes,counts):
        i=bycode[int(code)]
        lines.extend([f"    - _ingredientId: {i['ing_n']}",f"      _recipeCode: {code}",f"      _ingredientName: {q(i['ing_name'])}",f"      _amount: {count}"])
    lines.extend(['    _icon: {fileID: 0}',f"    _defaultUnlock: {int(r['type_n']==101)}",f"    _price: {r['price']}",'    _categories:','    - 2','    _grade: F',f"    _description: {q(r['m_name'])}"])
asset.write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f'Imported {len(ings)} ingredients and {len(recipes)} recipes.')
