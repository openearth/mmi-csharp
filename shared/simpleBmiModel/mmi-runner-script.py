#!x:\Python27\python.exe
# EASY-INSTALL-ENTRY-SCRIPT: 'mmi==0.1.6','console_scripts','mmi-runner'
__requires__ = 'mmi==0.1.6'
import sys
from pkg_resources import load_entry_point

if __name__ == '__main__':
    sys.exit(
        load_entry_point('mmi==0.1.6', 'console_scripts', 'mmi-runner')()
    )
