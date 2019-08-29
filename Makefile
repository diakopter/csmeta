# yes, it's a glorified shell script, but that's what people expect

all:
	xbuild /p:Configuration=Release Sprixel.sln
	cd Sprixel/bin/Release && mono Sprixel.exe && cd ../../..

clean:
	rm -rf Sprixel/bin Sprixel/obj

test: all
	cd Sprixel && sh run_tests.sh
