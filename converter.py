import json
import xml.etree.ElementTree as ET
from xml.dom import minidom
import requests
import argparse
from datetime import datetime
import os
from pathlib import Path

API_ENDPOINT = "https://usengprod-functionapp.azurewebsites.net/api/DataWipeResult?code=qaITPGBWPv55-nnUoXunopRqJIZeyHQwSbo0F0-aYOBTAzFua5QkRg=="
DEFAULTS = {
    "Username": "tester1",
    "StartTime": None,
    "EndTime": None,
    "Contract": "10083",
    "MachineName": "AudioTester",
    "TestArea": "MEXICALI_R2",
    "Program": "HP_MXLR2",
    "dbType": "TEST",
}
SUBTESTS = [
    "distorsion","left_dbfs","left_peak","right_dbfs","right_peak",
    "balance","volume","clipping","bluetooth","play_pausa",
    "anterior","siguiente","subir_volumen","bajar_volumen","resultado_mic"
]

def load_json(path):
    with open(path,'r',encoding='utf-8') as f:
        return json.load(f)

def find_input_file(input_arg):
    if input_arg:
        return input_arg

    target = 'final_results.json'
    cwd = Path.cwd()
    script_dir = Path(__file__).resolve().parent

    direct_candidates = [cwd / target, script_dir / target]
    for candidate in direct_candidates:
        if candidate.exists():
            return str(candidate)

    search_roots = [cwd]
    if script_dir != cwd:
        search_roots.append(script_dir)

    visited = set()
    for root in search_roots:
        if root in visited:
            continue
        visited.add(root)
        for dirpath, _, filenames in os.walk(root):
            if target in filenames:
                return str(Path(dirpath) / target)

    raise FileNotFoundError(f'Could not find {target}.')

def current_timestamp():
    return datetime.utcnow().strftime('%Y-%m-%d %H:%M:%S')

def build_xml(data):
    root = ET.Element('DataWipeResultV2')
    xdoc = ET.SubElement(root,'xDoc')
    rec = ET.SubElement(xdoc,'record')

    ET.SubElement(rec,'SerialNumber').text = str(data.get('serial',''))
    ET.SubElement(rec,'PartNumber').text = ''
    start_time = data.get('StartTime') or DEFAULTS['StartTime'] or current_timestamp()
    end_time = data.get('EndTime') or DEFAULTS['EndTime'] or start_time
    ET.SubElement(rec,'StartTime').text = start_time
    ET.SubElement(rec,'EndTime').text = end_time
    ET.SubElement(rec,'MachineName').text = DEFAULTS['MachineName']

    overall = 'PASS'
    for k,v in data.items():
        if k!='serial' and isinstance(v,str) and v.upper()=='FAIL':
            overall='FAIL'
            break
    ET.SubElement(rec,'Result').text = overall
    ET.SubElement(rec,'TestArea').text = DEFAULTS['TestArea']
    ET.SubElement(rec,'CellNumber').text = ''
    ET.SubElement(rec,'Program').text = DEFAULTS['Program']
    ET.SubElement(rec,'MiscInfo').text = ''
    ET.SubElement(rec,'MACAddress').text = ''
    ET.SubElement(rec,'Msg').text = ''
    ET.SubElement(rec,'LogFile').text = ''
    ET.SubElement(rec,'Username').text = str(data.get('Username') or DEFAULTS['Username'])
    ET.SubElement(rec,'OrderNumber').text = ''
    ET.SubElement(rec,'UploadTime').text = ''
    ET.SubElement(rec,'Contract').text = DEFAULTS['Contract']
    ET.SubElement(rec,'FileReference').text = ''
    ET.SubElement(rec,'FailureReference').text = ''
    ET.SubElement(rec,'FailureNumber').text = ''
    ET.SubElement(rec,'ErrItem').text = ''
    ET.SubElement(rec,'TestAreaOrig').text = ''
    ET.SubElement(rec,'BatteryHealthGrade').text = ''
    ET.SubElement(rec,'LogFileStatus').text = ''

    for idx,name in enumerate(SUBTESTS, start=1):
        if name not in data:
            continue
        st = ET.SubElement(rec,'subtest')
        ET.SubElement(st,'TestIDNumber').text = str(idx)
        ET.SubElement(st,'TestName').text = name
        ET.SubElement(st,'TestDesc').text = ''
        ET.SubElement(st,'StartTime').text = ''
        ET.SubElement(st,'EndTime').text = ''
        val = data[name]
        if isinstance(val, str):
            ET.SubElement(st,'Result').text = val
            ET.SubElement(st,'ErrorMessage').text = ''
            ET.SubElement(st,'ResultMessage').text = ''
        else:
            ET.SubElement(st,'Result').text = 'PASS'
            ET.SubElement(st,'ErrorMessage').text = ''
            ET.SubElement(st,'ResultMessage').text = str(val)

    ET.SubElement(root,'dbType').text = DEFAULTS['dbType']
    ET.SubElement(root,'servicename').text = ''
    ET.SubElement(root,'accesstoken').text = ''
    return root

def pretty_with_ns(elem):
    rough = ET.tostring(elem, encoding='unicode')
    dom = minidom.parseString(rough)
    xml = dom.toprettyxml(indent='    ')
    ns = 'http://winit/webservices/'
    xml = xml.replace('<DataWipeResultV2>', f'<ns0:DataWipeResultV2 xmlns:ns0="{ns}">')
    xml = xml.replace('</DataWipeResultV2>', '</ns0:DataWipeResultV2>')
    xml = xml.replace('<xDoc>', '<ns0:xDoc>').replace('</xDoc>','</ns0:xDoc>')
    xml = xml.replace('<dbType>','<ns0:dbType>').replace('</dbType>','</ns0:dbType>')
    xml = xml.replace('<servicename>','<ns0:servicename>').replace('</servicename>','</ns0:servicename>')
    xml = xml.replace('<accesstoken>','<ns0:accesstoken>').replace('</accesstoken>','</ns0:accesstoken>')
    return xml

def save(xml_str,path):
    with open(path,'w',encoding='utf-8') as f:
        f.write(xml_str)

def upload(xml_str):
    headers = {'Content-Type':'application/xml'}
    try:
        r = requests.post(API_ENDPOINT, data=xml_str, headers=headers, timeout=15)
        return r.status_code, r.text
    except requests.RequestException as exc:
        return None, str(exc)

def run():
    parser = argparse.ArgumentParser()
    parser.add_argument('--input', default=None)
    parser.add_argument('--output', default='final_results_converted.xml')
    parser.add_argument('--no-upload', action='store_true', help='skip API upload')
    parser.add_argument('--no-save', action='store_true')
    args = parser.parse_args()

    input_path = find_input_file(args.input)
    data = load_json(input_path)

    xml_elem = build_xml(data)
    xml_str = pretty_with_ns(xml_elem)

    if not args.no_save:
        save(xml_str, args.output)

    if not args.no_upload:
        code, text = upload(xml_str)
        if code is None:
            print('Upload status: FAILED')
            print('Upload error:', text)
        else:
            print('Upload status:', code)

if __name__=='__main__':
    run()
