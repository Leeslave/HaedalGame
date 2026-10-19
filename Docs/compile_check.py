"""Compile Unity project C# using installed Roslyn and the generated Unity references.
This is a static check, not a substitute for Unity import or EditMode/PlayMode tests.
"""
from pathlib import Path
import xml.etree.ElementTree as ET
import subprocess
root=Path(__file__).resolve().parents[1]
temp=root/'Temp'/'AITestCompile';temp.mkdir(parents=True,exist_ok=True)
csc=sorted(Path('C:/Program Files/dotnet/sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
for name in ['Assembly-CSharp','Assembly-CSharp-Editor']:
    project=ET.parse(root/(name+'.csproj')).getroot()
    defines=project.find('.//DefineConstants').text
    refs=[]
    for element in project.findall('.//Reference'):
        p=element.find('HintPath')
        if p is not None:
            path=Path(p.text)
            if not path.is_absolute():path=root/path
            if path.exists():refs.append(path)
    for p in project.findall('.//ProjectReference'):
        dll=temp/(Path(p.attrib['Include']).stem+'.dll')
        if not dll.exists():dll=root/'Library/ScriptAssemblies'/dll.name
        if dll.exists():refs.append(dll)
    sources=[]
    for e in project.findall('.//Compile'):
        p=root/e.attrib['Include']
        if p.exists():sources.append(p)
    for p in (root/'Assets/Scripts/Progression').rglob('*.cs'):
        if ('Editor' in p.parts)==(name=='Assembly-CSharp-Editor') and p not in sources:sources.append(p)
    args=['-nologo','-target:library','-langversion:9.0','-nostdlib+',f'-define:{defines}',f'-out:"{temp/name}.dll"']
    args+=['-r:"'+str(p)+'"' for p in refs]
    args+=['"'+str(p)+'"' for p in sources]
    rsp=temp/(name+'.rsp');rsp.write_text('\n'.join(args),encoding='utf-8-sig')
    result=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],cwd=root,capture_output=True)
    output=result.stdout.decode('utf-8',errors='replace')+result.stderr.decode('utf-8',errors='replace')
    (temp/(name+'.log')).write_text(output,encoding='utf-8')
    print(name + ': ' + ('PASS' if result.returncode == 0 else 'FAIL'))
    print('\n'.join(line for line in output.splitlines() if 'error ' in line).encode('ascii',errors='backslashreplace').decode())
    if result.returncode:raise SystemExit(result.returncode)
