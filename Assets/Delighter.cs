namespace OpenCvSharp.Demo
{
    using UnityEngine;
    using System.Collections;
    using OpenCvSharp;
    using UnityEngine.UI;

    using System;

    using SFB;

    using System.IO;

    using UnityEngine.Networking;

   

    public class Delighter : MonoBehaviour
    {
        public Texture2D inputTexture;
        public GameObject inputTextureGO;
        public GameObject blurryTexture;
        public GameObject domcolorTexture;
        public GameObject resultTexture;

        public Camera cam;
        public float blurSize = 64;
        private float originalBlurSize = 64;
        private int dominantR = 0;
        private int dominantG = 0;
        private int dominantB = 0;

        public GameObject outputTextureObject;

        private RawImage rawImageInput;
        private RawImage rawImageBlurry;
        private RawImage rawImageDomColor;
        private RawImage rawImageResult;

        public GameObject runButton;
        public GameObject saveButton;
        public GameObject blurKernelSizeTxtBox;
        public Text blurKernelSizeTxtBoxTxt;
        public GameObject outputFilenameTxtBox;
        public Text outputFilenameTxtBoxTxt;

        public GameObject blurSlider;
        public Text blurringStepTxt;
        public Text maxBlurSize;
        public Text colorExtractStepTxt;
        public Text finalResultStepTxt;

        public GameObject spinner;
        public GameObject resetBlurBtn;

        private bool movingSlider = false;

      

        Mat get_dominant_color(Mat samples, int _k = 4)
        {
            var bestLabels = new Mat();
            var centers = new Mat();
            Cv2.Kmeans(
                data: samples,
                k: _k,
                bestLabels: bestLabels,
                criteria:
                    new TermCriteria(type: CriteriaType.Eps | CriteriaType.MaxIter, maxCount: 10, epsilon: 1.0),
                attempts: 3,
                flags: KMeansFlags.PpCenters,
                centers: centers);

            return bestLabels;
        }

        public Mat PosterizedImage(Mat img, int colors)
        {
            // basics
            int attempts = 5;
            double eps = 0.01;
            TermCriteria criteria = new TermCriteria(CriteriaType.Eps | CriteriaType.MaxIter, attempts, eps);

            // prepare
            Mat labels = new Mat(), centers = new Mat();
            Mat samples = new Mat(img.Rows * img.Cols, 3, MatType.CV_32F);
            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    Vec3b color = img.At<Vec3b>(y, x);
                    for (int z = 0; z < 3; z++)
                        samples.Set<float>(y + x * img.Rows, z, color[z]);
                }
            }

            // run k-means
            Cv2.Kmeans(samples, colors, labels, criteria, attempts, KMeansFlags.PpCenters, centers);

            // restore original image
            Mat new_image = new Mat(img.Size(), img.Type());
            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    int cluster_idx = labels.At<int>(y + x * img.Rows, 0);
                    Vec3b color = new Vec3b(
                        (byte)centers.At<float>(cluster_idx, 0),
                        (byte)centers.At<float>(cluster_idx, 1),
                        (byte)centers.At<float>(cluster_idx, 2)
                    );

                    new_image.Set(y, x, color);
                }
            }

            Mat[] rgbPlanes;
            Cv2.Split(new_image, out rgbPlanes);

            // Calculate histogram
            Mat hist = new Mat();
            int[] hdims = { 256 }; // Histogram size for each dimension
            Rangef[] ranges = { new Rangef(0, 256), }; // min/max 
            Cv2.CalcHist(
                new Mat[] { img },
                new int[] { 0 },
                null,
                hist,
                1,
                hdims,
                ranges);

            // Get the max value of histogram
            double minVal, maxVal;

            Cv2.MinMaxLoc(rgbPlanes[0], out minVal, out maxVal);

            Debug.Log("Max B Val: " + maxVal);
            dominantR = (int)maxVal;

            Cv2.MinMaxLoc(rgbPlanes[1], out minVal, out maxVal);

            Debug.Log("Max G Val: " + maxVal);
            dominantG = (int)maxVal;

            Cv2.MinMaxLoc(rgbPlanes[2], out minVal, out maxVal);

            Debug.Log("Max R Val: " + maxVal);
            dominantB = (int)maxVal;


            return new_image;
        }

        public void getDominantRGB(Mat img)
        {
            Mat[] rgbPlanes;
            Cv2.Split(img, out rgbPlanes);

            // Calculate histogram
            Mat hist = new Mat();
            int[] hdims = { 256 }; // Histogram size for each dimension
            Rangef[] ranges = { new Rangef(0, 256), }; // min/max 
            Cv2.CalcHist(
                new Mat[] { img },
                new int[] { 0 },
                null,
                hist,
                1,
                hdims,
                ranges);

            // Get the max value of histogram
            double minVal, maxVal;

            Cv2.MinMaxLoc(rgbPlanes[0], out minVal, out maxVal);

            Debug.Log("Max B Val: " + maxVal);
            dominantR = (int)maxVal;

            Cv2.MinMaxLoc(rgbPlanes[1], out minVal, out maxVal);

            Debug.Log("Max G Val: " + maxVal);
            dominantG = (int)maxVal;

            Cv2.MinMaxLoc(rgbPlanes[2], out minVal, out maxVal);

            Debug.Log("Max R Val: " + maxVal);
            dominantB = (int)maxVal;
        }

        // Use this for initialization
        void Start()
        {
            rawImageInput = inputTextureGO.GetComponent<RawImage>();
            rawImageBlurry = blurryTexture.GetComponent<RawImage>();
            rawImageDomColor = domcolorTexture.GetComponent<RawImage>();
            rawImageResult = resultTexture.GetComponent<RawImage>();

            Debug.Log(outputFilenameTxtBoxTxt.text);

            outputFilenameTxtBox.GetComponent<InputField>().lineType = InputField.LineType.MultiLineSubmit;
            outputFilenameTxtBox.GetComponent<InputField>().text = UnityEngine.Application.dataPath + "/result.png";
        }

        public void resetGUI()
        {
            //Deactivate GUI Text
            deactivateText(blurringStepTxt);
            deactivateText(colorExtractStepTxt);
            deactivateText(finalResultStepTxt);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public float parseFloatWithLocale(string _floatStr)
        {
            float _value = 0.0f;
            float.TryParse(_floatStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _value);
            return _value;
        }

        public void openTextureFile()
        {
            Debug.Log("Open texture file panel");
            // Open file with filter
            var extensions = new[] {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
                new ExtensionFilter("Sound Files", "mp3", "wav" ),
                new ExtensionFilter("All Files", "*" ),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);

            Debug.Log("Paths: " + paths[0]);
            
            StartCoroutine(LoadTexture(paths[0]));
        }


        IEnumerator LoadTexture(string filepath)
        {
            Debug.Log("Loading image from: " + "file://" + filepath);

            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + filepath);
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                Texture texture = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
                this.rawImageInput.texture = texture;
                this.inputTexture = (Texture2D) texture;
                Debug.Log("Loading done");

                runButton.GetComponent<Button>().interactable = true;

                blurKernelSizeTxtBox.GetComponent<InputField>().interactable = true;
                //blurKernelSizeTxtBox.GetComponent<Text>().text = blurSize.ToString();
                blurSize = texture.width / Mathf.Pow(2, (4 - 1)) + texture.width / Mathf.Pow(2, 4);
                //blurSize = parseFloatWithLocale(blurSize);
                originalBlurSize = blurSize;
                Debug.Log("Blur size, when loading texture file: " + blurSize.ToString());
                //movingSlider = true; //prevent bug when moving slider value
                
                blurKernelSizeTxtBoxTxt.text = blurSize.ToString();
                updateBlurKernelGUI((Texture2D) this.rawImageInput.texture);
                outputFilenameTxtBox.GetComponent<InputField>().interactable = true;

                float maxBlur = Math.Max(this.rawImageInput.texture.width,this.rawImageInput.texture.height);
                maxBlurSize.text = maxBlur.ToString();
                Debug.Log("Max Blur Size: " + maxBlur.ToString());
                blurSlider.GetComponent<Slider>().interactable = true;
                blurSlider.GetComponent<Slider>().maxValue = maxBlur;
                blurSlider.GetComponent<Slider>().value = this.originalBlurSize;
                
            }
        }

        public void updateBlurKernelGUI(Texture2D texture)
        {
            Mat mat = Unity.TextureToMat(texture);
            blurSize = mat.Width / Mathf.Pow(2, (4 - 1)) + mat.Width / Mathf.Pow(2, 4);
            blurSize = Mathf.Floor(blurSize);
            originalBlurSize = blurSize;
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();

            resetBlurBtn.GetComponent<Button>().interactable = true;
            Debug.Log("Original blur size: " + originalBlurSize);
        }

        public void blurSizeValueChange(InputField val)
        {
            Debug.Log("val: " + val);
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();
            if(movingSlider) return; //prevent bugs where the slider is updating the textfield, in a loop
            val.text = val.text.Replace(',', '.'); //prevent problems with locales on commas in numbers
            Debug.Log("Value read from GUI: " + val.text);
            this.blurSize = parseFloatWithLocale(val.text);
            Debug.Log("Blur size changed to: " + blurSize);
            blurSlider.GetComponent<Slider>().value = this.blurSize;
        }

        public void blurSizeValueChange(Slider slider)
        {
            Debug.Log("Slider moved");
            this.blurSize = slider.value;
            Debug.Log("Blur size changed to: " + blurSize);
            //blurKernelSizeTxtBoxTxt.text = blurSize.ToString();
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();
        }

        public void startMovingSlider(Slider slider)
        {
            this.movingSlider = true;
            Debug.Log("Start moving the slider");
            Debug.Log("Slider moved");
            this.blurSize = slider.value;
            Debug.Log("slider.value: " + slider.value);
            Debug.Log("Blur size changed to: " + blurSize);
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();
        }

        public void draggingSlider(Slider slider)
        {
            Debug.Log("Moving the slider");
            this.blurSize = slider.value;
            Debug.Log("slider.value: " + slider.value);
            Debug.Log("Blur size changed to: " + blurSize);
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();
        }

        public void stopMovingSlider(Slider slider)
        {
            Debug.Log("Stop moving the slider");
            this.movingSlider = false;
        }

        public void resetBlurValue()
        {
            Debug.Log("Reset blur size to: " + originalBlurSize);
            blurSize = originalBlurSize;
            this.movingSlider = true;
            blurKernelSizeTxtBox.GetComponent<InputField>().text = blurSize.ToString();
            blurSlider.GetComponent<Slider>().value = blurSize;
        }

        public void activateText(Text textElement)
        {
            textElement.color = new Color(205.0f/255.0f,205.0f/255.0f,205.0f/255.0f);
        }

        public void deactivateText(Text textElement)
        {
            textElement.color = new Color(50.0f/255.0f,50.0f/255.0f,50.0f/255.0f);
        }

        public IEnumerator activateSpinner()
        {
            this.spinner.SetActive(true);
            yield return null;
        }

        public void deactivateSpinner()
        {
            this.spinner.SetActive(false);
        }

        public IEnumerator delighting()
        {
            Mat mat = Unity.TextureToMat(this.inputTexture);
            Mat grayMat = new Mat();
            Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
            Texture2D inputTexture = Unity.MatToTexture(grayMat);
            Debug.Log("Blur size: " + blurSize);

            /* Mat yuvMat = new Mat();
            Cv2.CvtColor(mat, yuvMat, ColorConversionCodes.BGR2YUV); */

            // blur image
            Mat blurred_img = new Mat(mat.Width, mat.Height, MatType.CV_8UC3);

            Cv2.Blur(mat, blurred_img, new OpenCvSharp.Size(blurSize, blurSize));
            displayTextureInUI(blurred_img, rawImageBlurry);

            //K-Means
            var columnVector = mat.Reshape(cn: 3, rows: mat.Rows * mat.Cols);
            // convert to floating point, it is a requirement of the k-means method of OpenCV.
            var samples = new Mat();
            columnVector.ConvertTo(samples, MatType.CV_32FC3);
            getDominantRGB(blurred_img);

            Mat result = new Mat();
            mat.CopyTo(result);

            Mat dom_color_bgr = new Mat();
            mat.CopyTo(dom_color_bgr);
            dom_color_bgr.SetTo(new Scalar(dominantR, dominantG, dominantB));
            displayTextureInUI(dom_color_bgr, rawImageDomColor);

            result = mat - blurred_img + dom_color_bgr;


            var matF = new Mat();
            mat.ConvertTo(matF, MatType.CV_32FC3);

            var blurredF = new Mat();
            blurred_img.ConvertTo(blurredF, MatType.CV_32FC3);

            var dom_color_bgrF = new Mat();
            dom_color_bgr.ConvertTo(dom_color_bgrF, MatType.CV_32FC3);

            Mat resultF = new Mat();
            dom_color_bgrF.CopyTo(resultF);

            resultF = matF - blurredF + dom_color_bgrF;

            resultF.ConvertTo(result, MatType.CV_8UC3);
            displayTextureInUI(result, rawImageResult);

            deactivateSpinner();
            saveButton.GetComponent<Button>().interactable = true;
            yield return null;
        }

        public void runDelighting()
        {
            StartCoroutine(activateSpinner());
            StartCoroutine(delighting());
        }

        public void displayTextureInUI(Mat mat, RawImage rawimage)
        {
            rawimage.texture = Unity.MatToTexture(mat);
        }

        public void saveResultFile()
        {
            Debug.Log("Save result as texture");

            File.WriteAllBytes(UnityEngine.Application.dataPath + "/blurred_img.png", ((Texture2D)rawImageBlurry.texture).EncodeToPNG());

            File.WriteAllBytes(UnityEngine.Application.dataPath + "/dom_color_bgr.png", ((Texture2D)rawImageDomColor.texture).EncodeToPNG());

            string outputFilename = "";
            if(outputFilenameTxtBox.GetComponent<InputField>().text != "")
            {
                outputFilename = outputFilenameTxtBox.GetComponent<InputField>().text;
            }
            else{
                outputFilename = UnityEngine.Application.dataPath + "/result.png";
            }
            File.WriteAllBytes(outputFilename, ((Texture2D)rawImageResult.texture).EncodeToPNG());


        }
    }
}