cd bin/Release
mono Sprixel.exe
prove -e 'mono -O=-all,cfold perlesque.exe' ../../t/*.t
