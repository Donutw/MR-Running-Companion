import os
import glob
import shutil


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DOCS_DIR = os.path.join(ROOT, "Docs")


def main():
    docxs = glob.glob(os.path.join(DOCS_DIR, "*.docx"))
    if not docxs:
        raise FileNotFoundError(f"No .docx files found in {DOCS_DIR}")

    # 选择生成出来的那份（一般只有一个）
    src = sorted(docxs)[0]
    dst = os.path.join(DOCS_DIR, "VR_System_Design_CN.docx")

    if os.path.abspath(src) != os.path.abspath(dst):
        shutil.copyfile(src, dst)

    print("SRC:", src)
    print("DST:", dst)


if __name__ == "__main__":
    main()

