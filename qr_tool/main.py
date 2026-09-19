"""批量二维码生成工具 - Python 版本"""
import os
import subprocess
import sys
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

import qrcode
from qrcode.constants import ERROR_CORRECT_Q
from PIL import Image, ImageTk


class QRCodeApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        root.title("批量二维码生成工具")
        root.geometry("900x650")
        root.resizable(False, False)

        # 当前预览页码（从 0 开始）
        self.preview_index = 0
        self.preview_images = []  # 预览用的 PIL Image 列表
        self.preview_tk_images = []  # 预览用的 ImageTk 引用，防止被 GC
        self._update_job = None  # 防抖定时器

        # 居中窗口
        root.update_idletasks()
        w = 900
        h = 650
        x = (root.winfo_screenwidth() - w) // 2
        y = (root.winfo_screenheight() - h) // 2
        root.geometry(f"{w}x{h}+{x}+{y}")

        self._build_ui()

    def _build_ui(self):
        # 二维码内容标签
        ttk.Label(self.root, text="二维码内容（每行一个）：").place(x=20, y=20)

        # 原始文件选择行
        self.text_lo_file = tk.StringVar()
        self.btn_brower_lo_file_text = tk.StringVar(value="选择原始文件")
        entry_lo_file = ttk.Entry(self.root, textvariable=self.text_lo_file, width=28, state="readonly")
        entry_lo_file.place(x=220, y=20, width=195)
        self.btn_brower_lo_file = ttk.Button(self.root, textvariable=self.btn_brower_lo_file_text,
                                             command=self.on_browse_lo_file)
        self.btn_brower_lo_file.place(x=420, y=18, width=95)

        # 二维码内容文本框
        self.text_content = tk.Text(self.root, wrap="none")
        self.text_content.place(x=20, y=48, width=500, height=120)
        scroll_content = ttk.Scrollbar(self.root, orient="vertical", command=self.text_content.yview)
        scroll_content.place(x=520, y=48, height=120)
        self.text_content.configure(yscrollcommand=scroll_content.set)
        # 内容变更时实时更新预览（防抖）
        self.text_content.bind("<<Modified>>", self._on_content_modified)

        # 文件名标签
        ttk.Label(self.root, text="自定义文件名（可选，与上面行对应）：").place(x=20, y=175)

        # 文件名文本框
        self.text_filename = tk.Text(self.root, wrap="none")
        self.text_filename.place(x=20, y=200, width=500, height=120)
        scroll_filename = ttk.Scrollbar(self.root, orient="vertical", command=self.text_filename.yview)
        scroll_filename.place(x=520, y=200, height=120)
        self.text_filename.configure(yscrollcommand=scroll_filename.set)
        # 文件名变更时也刷新预览（用于更新文件名预览）
        self.text_filename.bind("<<Modified>>", self._on_content_modified)

        # 保存路径
        ttk.Label(self.root, text="保存路径：").place(x=20, y=330)
        default_path = os.path.join(os.path.expanduser("~"), "Desktop", "QR_Codes")
        self.text_path = tk.StringVar(value=default_path)
        ttk.Entry(self.root, textvariable=self.text_path, width=45).place(x=100, y=330, width=320)
        ttk.Button(self.root, text="浏览...", command=self.on_browse).place(x=425, y=327, width=95)

        # 生成按钮
        self.btn_generate = tk.Button(self.root, text="生成二维码", command=self.on_generate,
                                      bg="#0078d4", fg="white", relief="flat")
        self.btn_generate.place(x=20, y=370, width=150, height=40)

        # 清空按钮
        ttk.Button(self.root, text="清空内容", command=self.on_clear).place(x=180, y=370, width=150, height=40)

        # 状态标签
        self.status_label = tk.Label(self.root, text="就绪", fg="green", anchor="w")
        self.status_label.place(x=20, y=430, width=500, height=20)

        # 分隔线
        tk.Frame(self.root, relief="ridge", bd=1).place(x=20, y=425, width=500, height=2)

        # 使用说明
        instruction = (
            "使用说明：\n"
            "1. 在上方文本框每行输入一个二维码内容\n"
            "2. 在下方文本框输入对应文件名（可选，不填则使用默认名称）\n"
            "3. 选择保存路径(不选则为默认路径)\n"
            "4. 点击生成按钮\n"
            "5. 可以选择需要转换成二维码的文档，每一行格式必须是:\n"
            '   "需要生成二维码的内容丨二维码图片名称"\n'
            "确认无异常后，选择保存路径，然后点击生成即可"
        )
        tk.Label(self.root, text=instruction, fg="red", justify="left", anchor="nw").place(x=20, y=460, width=500, height=150)

        # ===== 右侧预览面板 =====
        preview_frame = tk.LabelFrame(self.root, text="二维码实时预览", padx=5, pady=5)
        preview_frame.place(x=560, y=20, width=320, height=610)

        # 预览图片显示区域
        self.preview_label = tk.Label(preview_frame, text="暂无内容", fg="gray",
                                      width=30, height=15, bg="white", relief="sunken")
        self.preview_label.pack(fill="both", expand=True, padx=5, pady=5)

        # 文件名预览标签
        self.preview_name_label = tk.Label(preview_frame, text="", fg="#0078d4",
                                           font=("Microsoft YaHei", 10), anchor="w")
        self.preview_name_label.pack(fill="x", padx=5, pady=(5, 0))

        # 页码 / 计数标签
        self.page_label = tk.Label(preview_frame, text="0 / 0", fg="black")
        self.page_label.pack(pady=(5, 0))

        # 翻页按钮
        nav_frame = tk.Frame(preview_frame)
        nav_frame.pack(pady=10)
        self.btn_prev = ttk.Button(nav_frame, text="◀ 上一页", command=self.on_prev_page, state="disabled")
        self.btn_prev.pack(side="left", padx=5)
        self.btn_next = ttk.Button(nav_frame, text="下一页 ▶", command=self.on_next_page, state="disabled")
        self.btn_next.pack(side="left", padx=5)

    def _on_content_modified(self, event=None):
        """内容文本框变更时，防抖后刷新预览。"""
        # 重置 Modified 标记，以便下次再触发
        self.text_content.edit_modified(False)
        if self._update_job is not None:
            self.root.after_cancel(self._update_job)
        self._update_job = self.root.after(200, self.refresh_preview)

    def refresh_preview(self):
        """根据内容文本框的行，重新生成二维码预览列表并刷新当前页。"""
        self._update_job = None
        contents = [ln.strip() for ln in self.text_content.get("1.0", "end").splitlines() if ln.strip()]

        # 清空旧预览
        self.preview_images = []
        self.preview_tk_images = []

        # 逐行生成二维码缩略图（无内容则为空列表，不显示）
        for content in contents:
            img = self._make_preview_image(content)
            self.preview_images.append(img)

        # 计算文件名预览列表
        self.preview_names = self._build_file_names(contents)

        # 调整当前页码到有效范围
        if self.preview_images:
            if self.preview_index >= len(self.preview_images):
                self.preview_index = len(self.preview_images) - 1
            if self.preview_index < 0:
                self.preview_index = 0
        else:
            self.preview_index = 0

        self._show_preview()

    def _make_preview_image(self, content: str) -> Image.Image:
        """为单行内容生成预览二维码 PIL 图片。"""
        try:
            qr = qrcode.QRCode(
                version=None,
                error_correction=ERROR_CORRECT_Q,
                box_size=10,
                border=2,
            )
            qr.add_data(content)
            qr.make(fit=True)
            return qr.make_image(fill_color="black", back_color="white").convert("RGB")
        except Exception:
            # 生成失败返回空白占位图
            return Image.new("RGB", (200, 200), "white")

    def _show_preview(self):
        """在预览区显示当前页。"""
        total = len(self.preview_images)
        if total == 0:
            self.preview_label.config(image="", text="暂无内容")
            self.preview_name_label.config(text="")
            self.page_label.config(text="0 / 0")
            self.btn_prev.config(state="disabled")
            self.btn_next.config(state="disabled")
            return

        # 更新文件名预览
        name = self.preview_names[self.preview_index] if self.preview_index < len(self.preview_names) else ""
        self.preview_name_label.config(text=f"文件名：{name}")

        # 更新页码
        self.page_label.config(text=f"{self.preview_index + 1} / {total}")

        # 翻页按钮可用状态
        self.btn_prev.config(state="normal" if self.preview_index > 0 else "disabled")
        self.btn_next.config(state="normal" if self.preview_index < total - 1 else "disabled")

        # 缩放图片以适应显示区域
        img = self.preview_images[self.preview_index]
        disp_w, disp_h = 280, 280
        img.thumbnail((disp_w, disp_h), Image.LANCZOS)
        tk_img = ImageTk.PhotoImage(img)
        self.preview_tk_images.append(tk_img)
        self.preview_label.config(image=tk_img, text="")

    def on_prev_page(self):
        if self.preview_images and self.preview_index > 0:
            self.preview_index -= 1
            self._show_preview()

    def on_next_page(self):
        if self.preview_images and self.preview_index < len(self.preview_images) - 1:
            self.preview_index += 1
            self._show_preview()

    def on_browse(self):
        path = filedialog.askdirectory(
            title="选择二维码保存目录",
            initialdir=self.text_path.get() if os.path.isdir(self.text_path.get()) else os.path.expanduser("~"),
        )
        if path:
            self.text_path.set(path)

    def on_browse_lo_file(self):
        if self.btn_brower_lo_file_text.get() == "选择原始文件":
            path = filedialog.askopenfilename(
                title="选择需要生成二维码的源文件",
                filetypes=[("文档 (*.txt)", "*.txt"), ("所有文件 (*.*)", "*.*")],
            )
            if path:
                self.text_lo_file.set(path)
                messagebox.showinfo("首选文档选择成功提示",
                                    "首选文档已经选择完成，再点击(生成二维码内容与名称按钮)，生成多行二维码内容与名称")
        else:
            if self.text_lo_file.get():
                self._import_from_file(self.text_lo_file.get())

    def _import_from_file(self, file_path: str):
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                data = f.read()
        except UnicodeDecodeError:
            try:
                with open(file_path, "r", encoding="gbk") as f:
                    data = f.read()
            except Exception as ex:
                messagebox.showerror("错误", f"读取文件时出错: {ex}")
                return
        except Exception as ex:
            messagebox.showerror("错误", f"读取文件时出错: {ex}")
            return

        lines = data.splitlines()
        # 过滤掉空行
        lines = [ln for ln in lines if ln.strip() != ""]
        if not lines:
            self.text_lo_file.set("")
            return

        # 校验首行格式：内容丨名称
        first_parts = lines[0].split("丨")
        if len(first_parts) != 2:
            messagebox.showinfo("文档选择错误提示", "首选文档格式不满足要求，请重新选择!!!")
            self.text_lo_file.set("")
            return

        contents = []
        names = []
        for ln in lines:
            parts = ln.split("丨")
            if len(parts) != 2:
                continue
            contents.append(parts[0])
            names.append(parts[1])

        self.text_content.delete("1.0", "end")
        self.text_filename.delete("1.0", "end")
        self.text_content.insert("1.0", "\n".join(contents) + "\n")
        self.text_filename.insert("1.0", "\n".join(names) + "\n")
        self.preview_index = 0
        self.refresh_preview()

    def _clean_filename(self, name: str) -> str:
        invalid = '<>:"/\\|?*'
        for ch in invalid:
            name = name.replace(ch, "_")
        return name

    def _build_file_names(self, contents):
        """根据内容行与自定义文件名，生成最终文件名列表（含 .jpg 扩展名）。"""
        custom_names = [ln.strip() for ln in self.text_filename.get("1.0", "end").splitlines()]
        file_names = []
        for i in range(len(contents)):
            if i < len(custom_names) and custom_names[i]:
                name = self._clean_filename(custom_names[i])
                if not name:
                    name = f"二维码{i + 1}"
            else:
                name = f"二维码{i + 1}"
            if not name.lower().endswith(".jpg"):
                name += ".jpg"
            file_names.append(name)
        return file_names

    def on_generate(self):
        contents = [ln.strip() for ln in self.text_content.get("1.0", "end").splitlines() if ln.strip()]
        if not contents:
            messagebox.showwarning("提示", "请输入至少一行有效的二维码内容！")
            return

        save_path = self.text_path.get().strip()
        if not save_path:
            save_path = os.path.join(os.path.expanduser("~"), "Desktop", "QR_Codes")
            self.text_path.set(save_path)

        try:
            os.makedirs(save_path, exist_ok=True)
        except Exception as ex:
            messagebox.showerror("错误", f"无法创建目录：{ex}")
            return

        # 生成文件名列表
        file_names = self._build_file_names(contents)

        # 禁用生成按钮
        self.btn_generate.config(state="disabled", text="生成中...")
        self.status_label.config(text="正在生成二维码...")
        self.root.update_idletasks()

        success_count = 0
        try:
            qr = qrcode.QRCode(
                version=None,
                error_correction=ERROR_CORRECT_Q,
                box_size=20,
                border=1,
            )
            for i, content in enumerate(contents):
                self.status_label.config(text=f"正在生成第 {i + 1} 个二维码...")
                self.root.update_idletasks()

                qr.clear()
                qr.add_data(content)
                qr.make(fit=True)
                img = qr.make_image(fill_color="black", back_color="white")

                file_name = file_names[i]
                full_path = os.path.join(save_path, file_name)

                # 处理文件名重复
                counter = 1
                stem, ext = os.path.splitext(file_name)
                while os.path.exists(full_path):
                    file_name = f"{stem}_{counter}{ext}"
                    full_path = os.path.join(save_path, file_name)
                    counter += 1

                img.save(full_path, "JPEG")
                success_count += 1

            self.status_label.config(text=f"成功生成 {success_count} 个二维码！")

            # 生成成功时清空数据
            self.text_content.delete("1.0", "end")
            self.text_filename.delete("1.0", "end")
            self.text_lo_file.set("")
            self.preview_index = 0
            self.refresh_preview()

            if messagebox.askyesno("完成", f"成功生成 {success_count} 个二维码文件！\n保存位置：{save_path}\n是否打开保存目录？"):
                self._open_dir(save_path)

        except Exception as ex:
            messagebox.showerror("错误", f"生成过程中出错：{ex}")
            self.status_label.config(text="生成失败")
        finally:
            self.btn_generate.config(state="normal", text="生成二维码")

    def _open_dir(self, path: str):
        try:
            if sys.platform == "win32":
                os.startfile(path)
            elif sys.platform == "darwin":
                subprocess.Popen(["open", path])
            else:
                subprocess.Popen(["xdg-open", path])
        except Exception as ex:
            messagebox.showerror("错误", f"无法打开目录：{ex}")

    def on_clear(self):
        if messagebox.askyesno("确认", "确定要清空所有内容吗？"):
            self.text_content.delete("1.0", "end")
            self.text_filename.delete("1.0", "end")
            self.text_lo_file.set("")
            self.status_label.config(text="已清空")
            self.preview_index = 0
            self.refresh_preview()


def main():
    root = tk.Tk()
    app = QRCodeApp(root)

    def on_closing():
        if messagebox.askyesno("退出确认", "确定要退出程序吗？"):
            root.destroy()

    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()


if __name__ == "__main__":
    main()
