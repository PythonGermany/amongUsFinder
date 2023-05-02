# A fast and efficient script to search pixel art images for little amongi e.g. for the reddit r/place 2022 event

* Use image sequence: [program name].exe [source path] [destination path] [start index] [stop index] [index step]
* Use single image: [program name].exe [source file]

It is possible to process either single images or an image sequence named from 00000 to XXXXX.<br />
It is also possible to process every n-th image in the sequence: index step=2 says use only every 2nd picture in the sequence.<br />
The program also outputs a txt file containing the number of amongi counted in every image of the sequence.

The output images can be processed into a video file with other software.<br />
The results for the r/place 2022 event are like this:

[Watch a timelapse of the whole event here](https://youtu.be/mVmTI0B95Kk)
![Image](https://raw.githubusercontent.com/PythonGermany/amongUsFinder/main/image_assets/final_place_searched.png)
![Image](https://raw.githubusercontent.com/PythonGermany/amongUsFinder/main/image_assets/final_place.png)

