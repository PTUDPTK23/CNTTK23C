import os
from PIL import Image, ImageDraw, ImageFont

def wrap_text(text, font, max_width):
    """
    Wraps text character-by-character or word-by-word so it fits within max_width.
    """
    if not text:
        return [""]
    
    # Check if text already fits
    bbox = font.getbbox(text)
    if (bbox[2] - bbox[0]) <= max_width:
        return [text]
    
    # Try word wrapping first
    words = text.split(" ")
    lines = []
    current_line = ""
    
    for word in words:
        test_line = f"{current_line} {word}".strip()
        test_w = font.getbbox(test_line)[2] - font.getbbox(test_line)[0]
        if test_w <= max_width:
            current_line = test_line
        else:
            if current_line:
                lines.append(current_line)
            # Check if single word exceeds max_width (e.g. long JSON or path)
            word_w = font.getbbox(word)[2] - font.getbbox(word)[0]
            if word_w > max_width:
                # Wrap within word
                cur_part = ""
                for ch in word:
                    test_part = cur_part + ch
                    if (font.getbbox(test_part)[2] - font.getbbox(test_part)[0]) <= max_width:
                        cur_part = test_part
                    else:
                        lines.append(cur_part)
                        cur_part = ch
                current_line = cur_part
            else:
                current_line = word
                
    if current_line:
        lines.append(current_line)
        
    return lines

def render_terminal_window(output_path, title, lines, width=1536, height=864, font_size=15):
    """
    Renders a realistic full-screen Windows Terminal window on desktop (1536x864).
    Zero text overflow, perfect auto-wrap, authentic Windows styling.
    """
    font = ImageFont.truetype("consola.ttf", font_size)
    font_bold = ImageFont.truetype("consolab.ttf", font_size)
    ui_font = ImageFont.truetype("segoeui.ttf", 13)
    
    # Base desktop wallpaper: Windows 11 dark slate
    img = Image.new("RGB", (width, height), color=(20, 20, 24))
    draw = ImageDraw.Draw(img)
    
    # Terminal Window frame
    pad_top = 20
    pad_side = 20
    pad_bottom = 20
    win_w = width - pad_side * 2
    win_h = height - pad_top - pad_bottom
    
    # Window Shadow
    for s in range(6, 0, -1):
        alpha = 8
        draw.rectangle([pad_side - s, pad_top - s, pad_side + win_w + s, pad_top + win_h + s], outline=(10, 10, 12))
        
    # Main Terminal Window Body
    term_bg = (12, 12, 12)
    draw.rectangle([pad_side, pad_top, pad_side + win_w, pad_top + win_h], fill=term_bg)
    
    # Titlebar / Tab row
    tab_bar_h = 38
    draw.rectangle([pad_side, pad_top, pad_side + win_w, pad_top + tab_bar_h], fill=(30, 30, 33))
    
    # Tab
    tab_w = 320
    draw.rectangle([pad_side + 8, pad_top + 4, pad_side + 8 + tab_w, pad_top + tab_bar_h], fill=term_bg)
    # PowerShell icon & title
    draw.text((pad_side + 22, pad_top + 10), f">_  {title}", font=ui_font, fill=(235, 235, 235))
    # Close tab 'x'
    draw.text((pad_side + 8 + tab_w - 24, pad_top + 10), "×", font=ui_font, fill=(160, 160, 160))
    # Plus button for new tab
    draw.text((pad_side + 8 + tab_w + 14, pad_top + 8), "+", font=ImageFont.truetype("segoeui.ttf", 16), fill=(160, 160, 160))
    
    # Window controls (right)
    rx = pad_side + win_w
    # Minimize
    draw.line([rx - 120, pad_top + 18, rx - 108, pad_top + 18], fill=(190, 190, 190), width=1)
    # Maximize
    draw.rectangle([rx - 80, pad_top + 13, rx - 68, pad_top + 25], outline=(190, 190, 190), width=1)
    # Close
    draw.line([rx - 40, pad_top + 13, rx - 28, pad_top + 25], fill=(190, 190, 190), width=1)
    draw.line([rx - 28, pad_top + 13, rx - 40, pad_top + 25], fill=(190, 190, 190), width=1)
    
    # Terminal text area
    content_x = pad_side + 18
    content_y = pad_top + tab_bar_h + 14
    max_text_width = win_w - 36
    
    palette = {
        "white": (225, 225, 225),
        "gray": (155, 155, 155),
        "darkgray": (115, 115, 115),
        "prompt": (255, 255, 255),
        "green": (50, 205, 50),
        "cyan": (50, 210, 230),
        "yellow": (245, 215, 50),
        "red": (255, 80, 80),
    }
    
    line_h = int(font_size * 1.5)
    cy = content_y
    
    for item in lines:
        if isinstance(item, tuple):
            text, col_key = item
        else:
            text, col_key = str(item), "white"
            
        color = palette.get(col_key, (225, 225, 225))
        is_bold = col_key in ("prompt", "green", "red")
        cur_font = font_bold if is_bold else font
        
        # Auto-wrap lines that exceed max_text_width
        wrapped_lines = wrap_text(text, cur_font, max_text_width)
        for w_line in wrapped_lines:
            if cy + line_h < pad_top + win_h - 10:
                draw.text((content_x, cy), w_line, font=cur_font, fill=color)
                cy += line_h
                
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    img.save(output_path)
    print(f"Generated single window: {output_path}")

def render_split_terminal(output_path, title, panes, width=1536, height=864, font_size=14):
    """
    Renders realistic side-by-side terminal panes inside one Windows Terminal window.
    panes is a list of dicts: [ { 'title': '...', 'lines': [...] }, ... ]
    Each pane has its own independent coordinate system, clipping rect, and auto-wrap.
    Zero overflow between panes!
    """
    font = ImageFont.truetype("consola.ttf", font_size)
    font_bold = ImageFont.truetype("consolab.ttf", font_size)
    ui_font = ImageFont.truetype("segoeui.ttf", 13)
    
    img = Image.new("RGB", (width, height), color=(20, 20, 24))
    draw = ImageDraw.Draw(img)
    
    pad_top = 20
    pad_side = 20
    pad_bottom = 20
    win_w = width - pad_side * 2
    win_h = height - pad_top - pad_bottom
    
    # Shadow
    for s in range(6, 0, -1):
        draw.rectangle([pad_side - s, pad_top - s, pad_side + win_w + s, pad_top + win_h + s], outline=(10, 10, 12))
        
    term_bg = (12, 12, 12)
    draw.rectangle([pad_side, pad_top, pad_side + win_w, pad_top + win_h], fill=term_bg)
    
    # Titlebar
    tab_bar_h = 38
    draw.rectangle([pad_side, pad_top, pad_side + win_w, pad_top + tab_bar_h], fill=(30, 30, 33))
    
    tab_w = 340
    draw.rectangle([pad_side + 8, pad_top + 4, pad_side + 8 + tab_w, pad_top + tab_bar_h], fill=term_bg)
    draw.text((pad_side + 22, pad_top + 10), f">_  {title}", font=ui_font, fill=(235, 235, 235))
    draw.text((pad_side + 8 + tab_w - 24, pad_top + 10), "×", font=ui_font, fill=(160, 160, 160))
    draw.text((pad_side + 8 + tab_w + 14, pad_top + 8), "+", font=ImageFont.truetype("segoeui.ttf", 16), fill=(160, 160, 160))
    
    rx = pad_side + win_w
    draw.line([rx - 120, pad_top + 18, rx - 108, pad_top + 18], fill=(190, 190, 190), width=1)
    draw.rectangle([rx - 80, pad_top + 13, rx - 68, pad_top + 25], outline=(190, 190, 190), width=1)
    draw.line([rx - 40, pad_top + 13, rx - 28, pad_top + 25], fill=(190, 190, 190), width=1)
    draw.line([rx - 28, pad_top + 13, rx - 40, pad_top + 25], fill=(190, 190, 190), width=1)
    
    num_panes = len(panes)
    total_content_w = win_w
    content_y = pad_top + tab_bar_h
    content_h = win_h - tab_bar_h
    
    splitter_w = 4
    pane_w = (total_content_w - (num_panes - 1) * splitter_w) // num_panes
    
    palette = {
        "white": (225, 225, 225),
        "gray": (155, 155, 155),
        "darkgray": (115, 115, 115),
        "prompt": (255, 255, 255),
        "green": (50, 205, 50),
        "cyan": (50, 210, 230),
        "yellow": (245, 215, 50),
        "red": (255, 80, 80),
    }
    
    line_h = int(font_size * 1.5)
    
    for idx, pane in enumerate(panes):
        px = pad_side + idx * (pane_w + splitter_w)
        
        # Splitter bar between panes
        if idx > 0:
            draw.rectangle([px - splitter_w, content_y, px, content_y + content_h], fill=(45, 45, 50))
            
        # Create pane image to guarantee STRICT clipping
        pane_img = Image.new("RGB", (pane_w, content_h), color=term_bg)
        p_draw = ImageDraw.Draw(pane_img)
        
        # Pane mini header
        pane_header_h = 28
        p_draw.rectangle([0, 0, pane_w, pane_header_h], fill=(22, 22, 26))
        p_draw.text((12, 5), pane["title"], font=ui_font, fill=(180, 215, 255))
        
        # Content
        p_x = 12
        p_y = pane_header_h + 10
        max_p_w = pane_w - 24
        
        for item in pane["lines"]:
            text, col_key = item if isinstance(item, tuple) else (str(item), "white")
            color = palette.get(col_key, (225, 225, 225))
            is_bold = col_key in ("prompt", "green", "red")
            cur_font = font_bold if is_bold else font
            
            wrapped_lines = wrap_text(text, cur_font, max_p_w)
            for w_line in wrapped_lines:
                if p_y + line_h < content_h - 6:
                    p_draw.text((p_x, p_y), w_line, font=cur_font, fill=color)
                    p_y += line_h
                    
        # Paste pane onto main image
        img.paste(pane_img, (px, content_y))
        
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    img.save(output_path)
    print(f"Generated split terminal: {output_path}")
