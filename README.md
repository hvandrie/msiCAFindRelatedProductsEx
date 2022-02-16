Windows Installer natively evaluates ProductVersion as follows: major.minor.build

https://docs.microsoft.com/en-us/windows/win32/msi/productversion

Often software versions are formatted as: <Major.Minor.Revision.Build>

Which requires an need for 4th octet to be evaluated. msiCAFindRelatedProductsEx will handle 4 octets in similar manor as internal action FindRelatedProducts.

Instruction usage:

Compile using Visual Studio and WiX Toolset v3.x.
Import compiled msiCAFindRelatedProductsEx.CA.dll into Binary table of .msi and name it: msiCAFindRelatedProductsEx.dll
Define CustomAction in CustomAction table as follows:
Action Type Source Target

FindRelatedProductsEx 1 msiCAFindRelatedProductsEx.dll FindRelatedProductsEx

Schedule FindRelatedProductsEx CA in InstallExecuteSequence table right after FindRelatedProducts action
That's it. Use command line /lv* .log to find output and evaluation of ProductVersion Property and Upgrade table and it will populate matching ActionProperty properties defined in Upgrade table.
